import CoreGraphics
import Foundation
import ImageIO
import UniformTypeIdentifiers

// Final conveyor baker.
//
// The approved render remains the only visual source. The empty approved plate
// supplies the chassis and center rail, while each cavity is sampled directly
// from the approved populated render. Every runtime frame is a single
// transparent image, so Unity never stacks a stationary conveyor beneath the
// moving one and never reconstructs rims, shadows, or end caps procedurally.

private struct RGBA {
    var r: UInt8
    var g: UInt8
    var b: UInt8
    var a: UInt8

    static let clear = RGBA(r: 0, g: 0, b: 0, a: 0)
}

private struct ImageBuffer {
    let width: Int
    let height: Int
    var pixels: [UInt8]

    init(width: Int, height: Int, fill: RGBA = .clear) {
        self.width = width
        self.height = height
        pixels = [UInt8](repeating: 0, count: width * height * 4)
        if fill.a > 0 || fill.r > 0 || fill.g > 0 || fill.b > 0 {
            for y in 0..<height {
                for x in 0..<width {
                    self[x, y] = fill
                }
            }
        }
    }

    init(image: CGImage, width: Int, height: Int) {
        self.width = width
        self.height = height
        pixels = [UInt8](repeating: 0, count: width * height * 4)
        pixels.withUnsafeMutableBytes { bytes in
            guard let context = CGContext(
                data: bytes.baseAddress,
                width: width,
                height: height,
                bitsPerComponent: 8,
                bytesPerRow: width * 4,
                space: CGColorSpaceCreateDeviceRGB(),
                bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)
            else {
                fatalError("Could not create image context")
            }

            context.interpolationQuality = .high
            context.translateBy(x: 0, y: CGFloat(height))
            context.scaleBy(x: 1, y: -1)
            context.draw(image, in: CGRect(x: 0, y: 0, width: width, height: height))
        }
    }

    subscript(x: Int, y: Int) -> RGBA {
        get {
            let index = ((y * width) + x) * 4
            return RGBA(
                r: pixels[index],
                g: pixels[index + 1],
                b: pixels[index + 2],
                a: pixels[index + 3])
        }
        set {
            let index = ((y * width) + x) * 4
            pixels[index] = newValue.r
            pixels[index + 1] = newValue.g
            pixels[index + 2] = newValue.b
            pixels[index + 3] = newValue.a
        }
    }

    func makeImage() -> CGImage {
        let data = Data(pixels) as CFData
        guard let provider = CGDataProvider(data: data),
              let image = CGImage(
                width: width,
                height: height,
                bitsPerComponent: 8,
                bitsPerPixel: 32,
                bytesPerRow: width * 4,
                space: CGColorSpaceCreateDeviceRGB(),
                bitmapInfo: CGBitmapInfo(
                    rawValue: CGImageAlphaInfo.premultipliedLast.rawValue),
                provider: provider,
                decode: nil,
                shouldInterpolate: true,
                intent: .defaultIntent)
        else {
            fatalError("Could not create CGImage")
        }
        return image
    }

    func rotated180() -> ImageBuffer {
        var output = ImageBuffer(width: width, height: height)
        for y in 0..<height {
            for x in 0..<width {
                output[width - 1 - x, height - 1 - y] = self[x, y]
            }
        }
        return output
    }
}

private struct WorldPoint {
    let x: Double
    let y: Double
}

private struct SocketPatch {
    let image: ImageBuffer
}

private let sourceArtworkWidth = 797
private let sourceArtworkHeight = 180
private let artworkWidth = 797
private let artworkHeight = 207
private let frameWidth = 797
private let frameHeight = 207
private let frameCount = 192
private let phasePeriod = 1.0
private let cropRect = CGRect(x: 100, y: 125, width: 1970, height: 445)

// These dimensions match ConveyorArtworkPresenter. The 207-pixel transparent
// canvas has the same aspect ratio as 7.09 x 1.84 world units. The approved
// source is baked once into that native runtime canvas, so Unity applies one
// uniform scale and cannot distort individual animation frames differently.
private let targetWidth = 7.09
private let artworkLocalYOffset = -0.02
private let pixelsPerWorldUnit = Double(frameWidth) / targetWidth

private let patchSize = 64
private let socketHalfWidth = 20.0
private let socketHalfHeight = 22.5 *
    (Double(artworkHeight) / Double(sourceArtworkHeight))
private let socketCornerRadius = 8.0

// Pixel centers measured from the approved 797 x 180 crop. These are used only
// to isolate the exact approved cavity pixels. Runtime motion continues to use
// the existing mechanical path below.
private let sourceSocketCenters: [CGPoint] = [
    CGPoint(x: 729, y: 38),
    CGPoint(x: 665, y: 35),
    CGPoint(x: 598, y: 35),
    CGPoint(x: 532, y: 35),
    CGPoint(x: 466, y: 35),
    CGPoint(x: 399, y: 36),
    CGPoint(x: 333, y: 35),
    CGPoint(x: 266, y: 35),
    CGPoint(x: 199, y: 35),
    CGPoint(x: 131, y: 35),
    CGPoint(x: 68, y: 38),
    CGPoint(x: 28, y: 88),
    CGPoint(x: 68, y: 133),
    CGPoint(x: 131, y: 136),
    CGPoint(x: 198, y: 136),
    CGPoint(x: 266, y: 136),
    CGPoint(x: 333, y: 136),
    CGPoint(x: 399, y: 136),
    CGPoint(x: 465, y: 136),
    CGPoint(x: 532, y: 136),
    CGPoint(x: 598, y: 136),
    CGPoint(x: 665, y: 136),
    CGPoint(x: 728, y: 133),
    CGPoint(x: 769, y: 88)
]

private let approvedSocketCenters: [CGPoint] = sourceSocketCenters.map { point in
    CGPoint(
        x: point.x * Double(artworkWidth) / Double(sourceArtworkWidth),
        y: point.y * Double(artworkHeight) / Double(sourceArtworkHeight))
}

// Exact socket centers measured from the approved artwork after its final
// 180-degree visual orientation correction. ApprovedConveyorPath.cs contains
// the same points, keeping the baked cavities, marble anchors, and turn motion
// on one shared track.
private let mechanicalControlPoints: [WorldPoint] = [
    WorldPoint(x:  2.9312, y:  0.4288),
    WorldPoint(x:  2.3707, y:  0.4595),
    WorldPoint(x:  1.7747, y:  0.4595),
    WorldPoint(x:  1.1698, y:  0.4595),
    WorldPoint(x:  0.5738, y:  0.4595),
    WorldPoint(x: -0.0133, y:  0.4595),
    WorldPoint(x: -0.6005, y:  0.4595),
    WorldPoint(x: -1.1965, y:  0.4595),
    WorldPoint(x: -1.7836, y:  0.4595),
    WorldPoint(x: -2.3796, y:  0.4595),
    WorldPoint(x: -2.9401, y:  0.4288),
    WorldPoint(x: -3.3048, y: -0.0316),
    WorldPoint(x: -2.9490, y: -0.5431),
    WorldPoint(x: -2.3796, y: -0.5738),
    WorldPoint(x: -1.7836, y: -0.5738),
    WorldPoint(x: -1.1965, y: -0.5738),
    WorldPoint(x: -0.6094, y: -0.5738),
    WorldPoint(x: -0.0133, y: -0.5635),
    WorldPoint(x:  0.5738, y: -0.5738),
    WorldPoint(x:  1.1698, y: -0.5738),
    WorldPoint(x:  1.7658, y: -0.5738),
    WorldPoint(x:  2.3707, y: -0.5738),
    WorldPoint(x:  2.9312, y: -0.5431),
    WorldPoint(x:  3.2870, y: -0.0316)
]

private func loadImage(_ path: String) -> CGImage {
    let url = URL(fileURLWithPath: path) as CFURL
    guard let source = CGImageSourceCreateWithURL(url, nil),
          let image = CGImageSourceCreateImageAtIndex(source, 0, nil)
    else {
        fatalError("Could not load \(path)")
    }
    return image
}

private func crop(_ image: CGImage, to rect: CGRect) -> CGImage {
    guard let cropped = image.cropping(to: rect) else {
        fatalError("Could not crop source image")
    }
    return cropped
}

private func clamp(_ value: Double, _ lower: Double = 0, _ upper: Double = 1) -> Double {
    return max(lower, min(upper, value))
}

private func smoothstep(_ edge0: Double, _ edge1: Double, _ value: Double) -> Double {
    if edge0 == edge1 { return value < edge0 ? 0 : 1 }
    let t = clamp((value - edge0) / (edge1 - edge0))
    return t * t * (3 - (2 * t))
}

private func roundedBoxDistance(
    x: Double,
    y: Double,
    halfWidth: Double,
    halfHeight: Double,
    radius: Double
) -> Double {
    let dx = abs(x) - halfWidth + radius
    let dy = abs(y) - halfHeight + radius
    let outsideX = max(dx, 0)
    let outsideY = max(dy, 0)
    return min(max(dx, dy), 0) +
        sqrt((outsideX * outsideX) + (outsideY * outsideY)) - radius
}

private func coverage(distance: Double, feather: Double = 1.0) -> Double {
    return 1 - smoothstep(-feather, feather, distance)
}

private func sample(_ image: ImageBuffer, x: Double, y: Double) -> RGBA {
    let safeX = clamp(x, 0, Double(image.width - 1))
    let safeY = clamp(y, 0, Double(image.height - 1))
    let x0 = Int(floor(safeX))
    let y0 = Int(floor(safeY))
    let x1 = min(x0 + 1, image.width - 1)
    let y1 = min(y0 + 1, image.height - 1)
    let tx = safeX - Double(x0)
    let ty = safeY - Double(y0)

    let p00 = image[x0, y0]
    let p10 = image[x1, y0]
    let p01 = image[x0, y1]
    let p11 = image[x1, y1]

    func channel(_ a: UInt8, _ b: UInt8, _ c: UInt8, _ d: UInt8) -> UInt8 {
        let top = (Double(a) * (1 - tx)) + (Double(b) * tx)
        let bottom = (Double(c) * (1 - tx)) + (Double(d) * tx)
        return UInt8(clamp((top * (1 - ty)) + (bottom * ty), 0, 255))
    }

    return RGBA(
        r: channel(p00.r, p10.r, p01.r, p11.r),
        g: channel(p00.g, p10.g, p01.g, p11.g),
        b: channel(p00.b, p10.b, p01.b, p11.b),
        a: channel(p00.a, p10.a, p01.a, p11.a))
}

private func premultiply(_ color: RGBA, opacity: Double) -> RGBA {
    let alpha = clamp(opacity) * (Double(color.a) / 255)
    return RGBA(
        r: UInt8(clamp(Double(color.r) * alpha, 0, 255)),
        g: UInt8(clamp(Double(color.g) * alpha, 0, 255)),
        b: UInt8(clamp(Double(color.b) * alpha, 0, 255)),
        a: UInt8(clamp(alpha * 255, 0, 255)))
}

private func scalePremultiplied(_ color: RGBA, opacity: Double) -> RGBA {
    let scale = clamp(opacity)
    return RGBA(
        r: UInt8(clamp(Double(color.r) * scale, 0, 255)),
        g: UInt8(clamp(Double(color.g) * scale, 0, 255)),
        b: UInt8(clamp(Double(color.b) * scale, 0, 255)),
        a: UInt8(clamp(Double(color.a) * scale, 0, 255)))
}

private func composite(_ top: RGBA, over bottom: RGBA) -> RGBA {
    let topAlpha = Double(top.a) / 255
    let bottomAlpha = Double(bottom.a) / 255
    let inverse = 1 - topAlpha
    let outputAlpha = topAlpha + (bottomAlpha * inverse)
    return RGBA(
        r: UInt8(clamp(Double(top.r) + (Double(bottom.r) * inverse), 0, 255)),
        g: UInt8(clamp(Double(top.g) + (Double(bottom.g) * inverse), 0, 255)),
        b: UInt8(clamp(Double(top.b) + (Double(bottom.b) * inverse), 0, 255)),
        a: UInt8(clamp(outputAlpha * 255, 0, 255)))
}

private func mixPremultiplied(_ first: RGBA, _ second: RGBA, amount: Double) -> RGBA {
    let t = clamp(amount)
    let inverse = 1 - t
    return RGBA(
        r: UInt8(clamp((Double(first.r) * inverse) + (Double(second.r) * t), 0, 255)),
        g: UInt8(clamp((Double(first.g) * inverse) + (Double(second.g) * t), 0, 255)),
        b: UInt8(clamp((Double(first.b) * inverse) + (Double(second.b) * t), 0, 255)),
        a: UInt8(clamp((Double(first.a) * inverse) + (Double(second.a) * t), 0, 255)))
}

private func approvedObjectAlpha(_ pixel: RGBA, x: Int, y: Int) -> Double {
    let aspect = 1970.0 / 445.0
    let localX = (Double(x) + 0.5) / Double(artworkWidth)
    let localY = 1 - ((Double(y) + 0.5) / Double(artworkHeight))
    let pointX = (localX - 0.5) * aspect + 0.001
    let pointY = localY - 0.5 + 0.007
    let silhouette = coverage(
        distance: roundedBoxDistance(
            x: pointX,
            y: pointY,
            halfWidth: 2.183,
            halfHeight: 0.464,
            radius: 0.464),
        feather: 1.1 / Double(artworkHeight))

    // Remove the saturated preview-blue matte without erasing the lavender or
    // pearl pixels belonging to the approved chassis.
    let blueExcess = max(
        0,
        Double(pixel.b) - max(Double(pixel.r), Double(pixel.g)))
    let backgroundAmount = smoothstep(45, 80, blueExcess)
    return clamp(silhouette * (1 - backgroundAmount))
}

private func beltInteriorAlpha(canvasX: Double, canvasY: Double) -> Double {
    let aspect = 1970.0 / 445.0
    let localX = (canvasX + 0.5) / Double(artworkWidth)
    let localY = 1 - ((canvasY + 0.5) / Double(artworkHeight))
    let pointX = (localX - 0.5) * aspect + 0.001
    let pointY = localY - 0.5 + 0.007
    return coverage(
        distance: roundedBoxDistance(
            x: pointX,
            y: pointY,
            halfWidth: 2.134,
            halfHeight: 0.415,
            radius: 0.415),
        feather: 1.1 / Double(artworkHeight))
}

private func buildApprovedBase(clean: ImageBuffer) -> ImageBuffer {
    var output = ImageBuffer(width: frameWidth, height: frameHeight)
    for sourceY in 0..<artworkHeight {
        for x in 0..<artworkWidth {
            let source = clean[x, sourceY]
            let alpha = approvedObjectAlpha(source, x: x, y: sourceY)
            output[x, sourceY] = premultiply(source, opacity: alpha)
        }
    }
    return output
}

private func centeredAngle(at index: Int, points: [CGPoint]) -> Double {
    let previous = points[(index - 1 + points.count) % points.count]
    let next = points[(index + 1) % points.count]
    return atan2(next.y - previous.y, next.x - previous.x)
}

private func extractSocket(
    from approved: ImageBuffer,
    center: CGPoint,
    angle: Double
) -> SocketPatch {
    var patch = ImageBuffer(width: patchSize, height: patchSize)
    let patchCenter = Double(patchSize - 1) * 0.5
    let cosine = cos(angle)
    let sine = sin(angle)

    for y in 0..<patchSize {
        for x in 0..<patchSize {
            let localX = Double(x) - patchCenter
            let localY = Double(y) - patchCenter
            let distance = roundedBoxDistance(
                x: localX,
                y: localY,
                halfWidth: socketHalfWidth,
                halfHeight: socketHalfHeight,
                radius: socketCornerRadius)
            let mask = coverage(distance: distance, feather: 0.85)
            if mask <= 0 { continue }

            let sourceX = center.x + (cosine * localX) - (sine * localY)
            let sourceY = center.y + (sine * localX) + (cosine * localY)
            patch[x, y] = premultiply(sample(approved, x: sourceX, y: sourceY), opacity: mask)
        }
    }
    return SocketPatch(image: patch)
}

private func catmullRomPose(_ parameter: Double) -> (WorldPoint, WorldPoint) {
    let count = mechanicalControlPoints.count
    let wrapped = parameter - floor(parameter / Double(count)) * Double(count)
    let segment = Int(floor(wrapped)) % count
    let t = wrapped - floor(wrapped)
    let p0 = mechanicalControlPoints[(segment - 1 + count) % count]
    let p1 = mechanicalControlPoints[segment]
    let p2 = mechanicalControlPoints[(segment + 1) % count]
    let p3 = mechanicalControlPoints[(segment + 2) % count]
    let t2 = t * t
    let t3 = t2 * t

    let position = WorldPoint(
        x: 0.5 * (
            (2 * p1.x) + ((-p0.x + p2.x) * t) +
            (((2 * p0.x) - (5 * p1.x) + (4 * p2.x) - p3.x) * t2) +
            ((-p0.x + (3 * p1.x) - (3 * p2.x) + p3.x) * t3)),
        y: 0.5 * (
            (2 * p1.y) + ((-p0.y + p2.y) * t) +
            (((2 * p0.y) - (5 * p1.y) + (4 * p2.y) - p3.y) * t2) +
            ((-p0.y + (3 * p1.y) - (3 * p2.y) + p3.y) * t3)))

    var tangent = WorldPoint(
        x: 0.5 * (
            (-p0.x + p2.x) +
            (2 * ((2 * p0.x) - (5 * p1.x) + (4 * p2.x) - p3.x) * t) +
            (3 * (-p0.x + (3 * p1.x) - (3 * p2.x) + p3.x) * t2)),
        y: 0.5 * (
            (-p0.y + p2.y) +
            (2 * ((2 * p0.y) - (5 * p1.y) + (4 * p2.y) - p3.y) * t) +
            (3 * (-p0.y + (3 * p1.y) - (3 * p2.y) + p3.y) * t2)))

    let magnitude = sqrt((tangent.x * tangent.x) + (tangent.y * tangent.y))
    if magnitude > 0.000_001 {
        tangent = WorldPoint(x: tangent.x / magnitude, y: tangent.y / magnitude)
    } else {
        tangent = WorldPoint(x: -1, y: 0)
    }
    return (position, tangent)
}

private func renderSocket(
    topPatch: SocketPatch,
    bottomPatch: SocketPatch,
    bottomBlend: Double,
    into frame: inout ImageBuffer,
    centerX: Double,
    centerY: Double,
    angle: Double
) {
    let radius = 35
    let minimumX = max(0, Int(floor(centerX)) - radius)
    let maximumX = min(frame.width - 1, Int(ceil(centerX)) + radius)
    let minimumY = max(0, Int(floor(centerY)) - radius)
    let maximumY = min(frame.height - 1, Int(ceil(centerY)) + radius)
    let patchCenter = Double(patchSize - 1) * 0.5
    let cosine = cos(angle)
    let sine = sin(angle)

    for y in minimumY...maximumY {
        for x in minimumX...maximumX {
            let deltaX = (Double(x) + 0.5) - centerX
            let deltaY = (Double(y) + 0.5) - centerY
            let localX = (cosine * deltaX) + (sine * deltaY)
            let localY = (-sine * deltaX) + (cosine * deltaY)
            let patchX = localX + patchCenter
            let patchY = localY + patchCenter
            if patchX < 0 || patchX > Double(patchSize - 1) ||
                patchY < 0 || patchY > Double(patchSize - 1) {
                continue
            }

            let topSample = sample(topPatch.image, x: patchX, y: patchY)
            let bottomSample = sample(bottomPatch.image, x: patchX, y: patchY)
            var top = mixPremultiplied(topSample, bottomSample, amount: bottomBlend)
            if top.a == 0 { continue }
            let clip = beltInteriorAlpha(canvasX: Double(x), canvasY: Double(y))
            if clip <= 0 { continue }
            if clip < 1 {
                top = scalePremultiplied(top, opacity: clip)
            }
            frame[x, y] = composite(top, over: frame[x, y])
        }
    }
}

private func renderFrame(
    base: ImageBuffer,
    phase: Double
) -> ImageBuffer {
    var frame = base
    for index in 0..<mechanicalControlPoints.count {
        let pose = catmullRomPose(Double(index) + (phase * 24.0))
        let centerX = (Double(frameWidth) * 0.5) + (pose.0.x * pixelsPerWorldUnit)
        let centerY = (Double(frameHeight) * 0.5) -
            ((pose.0.y - artworkLocalYOffset) * pixelsPerWorldUnit)
        let canvasAngle = -atan2(pose.1.y, pose.1.x)
        let isLight = isLightSocket(index)
        let topPatch = isLight ? lightTopSocketPatch : darkTopSocketPatch
        let bottomPatch = isLight ? lightBottomSocketPatch : darkBottomSocketPatch
        let bottomBlend = smoothstep(-0.18, 0.18, pose.0.y)
        renderSocket(
            topPatch: topPatch,
            bottomPatch: bottomPatch,
            bottomBlend: bottomBlend,
            into: &frame,
            centerX: centerX,
            centerY: centerY,
            angle: canvasAngle)
    }
    return frame
}

private func writePNG(_ image: CGImage, to path: String) {
    let url = URL(fileURLWithPath: path) as CFURL
    guard let destination = CGImageDestinationCreateWithURL(
        url,
        UTType.png.identifier as CFString,
        1,
        nil)
    else {
        fatalError("Could not create PNG destination")
    }
    CGImageDestinationAddImage(destination, image, nil)
    guard CGImageDestinationFinalize(destination) else {
        fatalError("Could not write \(path)")
    }
}

guard CommandLine.arguments.count == 4 else {
    fatalError(
        "Usage: bake_clean_conveyor_frames.swift " +
        "<approved.png> <approved-clean-plate.png> <output-folder>")
}

let approvedPath = CommandLine.arguments[1]
let cleanPath = CommandLine.arguments[2]
let outputFolder = CommandLine.arguments[3]
try FileManager.default.createDirectory(
    atPath: outputFolder,
    withIntermediateDirectories: true)

private let approvedCrop = crop(loadImage(approvedPath), to: cropRect)
private let cleanCrop = crop(loadImage(cleanPath), to: cropRect)
private let approved = ImageBuffer(
    image: approvedCrop,
    width: artworkWidth,
    height: artworkHeight)
private let clean = ImageBuffer(
    image: cleanCrop,
    width: artworkWidth,
    height: artworkHeight)
private let base = buildApprovedBase(clean: clean).rotated180()
private func isLightSocket(_ index: Int) -> Bool {
    let wrapped = ((index % 24) + 24) % 24
    return wrapped == 0 ||
        wrapped == 4 || wrapped == 5 || wrapped == 6 ||
        wrapped == 10 || wrapped == 12 ||
        wrapped == 16 || wrapped == 17 || wrapped == 18 ||
        wrapped == 22
}

private let darkTopSocketPatch = extractSocket(
    from: approved,
    center: approvedSocketCenters[1],
    angle: centeredAngle(at: 1, points: approvedSocketCenters))
private let lightTopSocketPatch = extractSocket(
    from: approved,
    center: approvedSocketCenters[4],
    angle: centeredAngle(at: 4, points: approvedSocketCenters))
private let darkBottomSocketPatch = extractSocket(
    from: approved,
    center: approvedSocketCenters[13],
    angle: centeredAngle(at: 13, points: approvedSocketCenters))
private let lightBottomSocketPatch = extractSocket(
    from: approved,
    center: approvedSocketCenters[16],
    angle: centeredAngle(at: 16, points: approvedSocketCenters))

for index in 0..<frameCount {
    let phase = phasePeriod * Double(index) / Double(frameCount)
    let frame = renderFrame(base: base, phase: phase)
    let filename = String(format: "ConveyorFrame_%03d.png", index)
    writePNG(
        frame.makeImage(),
        to: URL(fileURLWithPath: outputFolder).appendingPathComponent(filename).path)
}

writePNG(
    base.makeImage(),
    to: URL(fileURLWithPath: outputFolder)
        .appendingPathComponent("ConveyorCleanBase.png").path)
writePNG(
    buildApprovedBase(clean: approved).makeImage(),
    to: URL(fileURLWithPath: outputFolder)
        .appendingPathComponent("ConveyorApprovedOriginal.png").path)
print(
    "Baked \(frameCount) single-layer approved conveyor frames at " +
    "\(frameWidth)x\(frameHeight), with no background matte or duplicate render layers.")

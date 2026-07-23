import CoreGraphics
import Foundation
import ImageIO
import UniformTypeIdentifiers

// Bakes the approved filled 3x3 tray into color-matched, single-layer runtime
// frames. Unity only swaps the baked frame as balls release; it never rebuilds
// the approved shell, rim, front wall, outline, lighting, or ball packing.

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

    init(image: CGImage) {
        width = image.width
        height = image.height
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

    func flippedVertically() -> ImageBuffer {
        var output = ImageBuffer(width: width, height: height)
        for y in 0..<height {
            for x in 0..<width {
                output[x, height - 1 - y] = self[x, y]
            }
        }
        return output
    }
}

private struct ColorVariant {
    let name: String
    let targetHue: Double
    let saturationScale: Double
    let valueScale: Double
    let preserveGreen: Bool
}

private let variants = [
    ColorVariant(name: "Green", targetHue: 0.285, saturationScale: 1.0, valueScale: 1.0, preserveGreen: true),
    ColorVariant(name: "Blue", targetHue: 0.625, saturationScale: 1.20, valueScale: 1.05, preserveGreen: false),
    ColorVariant(name: "Orange", targetHue: 0.070, saturationScale: 1.18, valueScale: 1.08, preserveGreen: false),
    ColorVariant(name: "Yellow", targetHue: 0.145, saturationScale: 1.00, valueScale: 1.16, preserveGreen: false),
    ColorVariant(name: "Pink", targetHue: 0.925, saturationScale: 1.10, valueScale: 1.08, preserveGreen: false)
]

private let sourceSize = 1254
private let cropX = 112
private let cropY = 118
private let cropWidth = 1030
private let cropHeight = 996
private let outputWidth = 512
private let outputHeight = 495
private let releaseOrder = [6, 7, 8, 3, 4, 5, 0, 1, 2]

private let ballCentersX = [350.0, 625.0, 900.0]
private let ballCentersY = [322.0, 570.0, 810.0]

private func clamp(_ value: Double, _ lower: Double = 0, _ upper: Double = 1) -> Double {
    max(lower, min(upper, value))
}

private func smoothstep(_ edge0: Double, _ edge1: Double, _ value: Double) -> Double {
    if edge0 == edge1 { return value < edge0 ? 0 : 1 }
    let t = clamp((value - edge0) / (edge1 - edge0))
    return t * t * (3 - (2 * t))
}

private func loadImage(_ path: String) -> CGImage {
    let url = URL(fileURLWithPath: path) as CFURL
    guard let source = CGImageSourceCreateWithURL(url, nil),
          let image = CGImageSourceCreateImageAtIndex(source, 0, nil)
    else {
        fatalError("Could not load \(path)")
    }
    return image
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

private func mix(_ first: RGBA, _ second: RGBA, amount: Double) -> RGBA {
    let t = clamp(amount)
    let inverse = 1 - t
    return RGBA(
        r: UInt8(clamp((Double(first.r) * inverse) + (Double(second.r) * t), 0, 255)),
        g: UInt8(clamp((Double(first.g) * inverse) + (Double(second.g) * t), 0, 255)),
        b: UInt8(clamp((Double(first.b) * inverse) + (Double(second.b) * t), 0, 255)),
        a: 255)
}

private func composite(_ top: RGBA, over bottom: RGBA) -> RGBA {
    let topAlpha = Double(top.a) / 255
    let inverse = 1 - topAlpha
    return RGBA(
        r: UInt8(clamp(Double(top.r) + (Double(bottom.r) * inverse), 0, 255)),
        g: UInt8(clamp(Double(top.g) + (Double(bottom.g) * inverse), 0, 255)),
        b: UInt8(clamp(Double(top.b) + (Double(bottom.b) * inverse), 0, 255)),
        a: UInt8(clamp((topAlpha + ((Double(bottom.a) / 255) * inverse)) * 255, 0, 255)))
}

private func interiorWeight(x: Int, y: Int) -> Double {
    let horizontal = smoothstep(155, 178, Double(x)) *
        (1 - smoothstep(1065, 1088, Double(x)))
    let vertical = smoothstep(150, 178, Double(y)) *
        (1 - smoothstep(930, 958, Double(y)))
    return horizontal * vertical
}

private func removedBallWeight(index: Int, x: Int, y: Int) -> Double {
    let column = index % 3
    let row = index / 3
    let px = Double(x) + 0.5
    let py = Double(y) + 0.5
    let dx = abs((px - ballCentersX[column]) / 168.0)
    let dy = abs((py - ballCentersY[row]) / 166.0)
    let ellipse = (dx * dx) + (dy * dy)
    return (1 - smoothstep(0.82, 1.03, ellipse)) *
        interiorWeight(x: x, y: y)
}

private func makeOccupancyFrame(
    filled: ImageBuffer,
    empty: ImageBuffer,
    remainingCount: Int
) -> ImageBuffer {
    if remainingCount >= 9 {
        return filled
    }

    var frame = filled
    let removed = Set(releaseOrder.prefix(9 - max(0, remainingCount)))
    for y in 0..<filled.height {
        for x in 0..<filled.width {
            var removal = 0.0
            for index in removed {
                removal = max(
                    removal,
                    removedBallWeight(index: index, x: x, y: y))
            }
            if remainingCount <= 0 {
                removal = max(removal, interiorWeight(x: x, y: y))
            }
            if removal > 0 {
                frame[x, y] = mix(filled[x, y], empty[x, y], amount: removal)
            }
        }
    }
    return frame
}

private func buildSilhouetteAlpha(from filled: ImageBuffer) -> [Double] {
    var minimumX = [Int](repeating: Int.max, count: filled.height)
    var maximumX = [Int](repeating: Int.min, count: filled.height)
    var firstRow = filled.height
    var lastRow = -1

    for y in 0..<filled.height {
        for x in 0..<filled.width {
            let pixel = filled[x, y]
            let green = Double(pixel.g)
            if green > 65 && green > Double(pixel.r) + 13 && green > Double(pixel.b) + 13 {
                minimumX[y] = min(minimumX[y], x)
                maximumX[y] = max(maximumX[y], x)
                firstRow = min(firstRow, y)
                lastRow = max(lastRow, y)
            }
        }
    }

    guard firstRow <= lastRow else {
        fatalError("Could not isolate the green approved tray")
    }

    var alpha = [Double](repeating: 0, count: filled.width * filled.height)
    for y in max(0, firstRow - 6)...min(filled.height - 1, lastRow + 6) {
        var sampleY = min(max(y, firstRow), lastRow)
        while minimumX[sampleY] == Int.max && sampleY > firstRow { sampleY -= 1 }
        if minimumX[sampleY] == Int.max { continue }

        let left = Double(max(0, minimumX[sampleY] - 6))
        let right = Double(min(filled.width - 1, maximumX[sampleY] + 6))
        for x in max(0, Int(left) - 1)...min(filled.width - 1, Int(right) + 1) {
            let horizontal = smoothstep(left - 1.0, left + 0.75, Double(x)) *
                (1 - smoothstep(right - 0.75, right + 1.0, Double(x)))
            let verticalTop = smoothstep(Double(firstRow - 7), Double(firstRow - 4), Double(y))
            let verticalBottom = 1 - smoothstep(Double(lastRow + 4), Double(lastRow + 7), Double(y))
            alpha[(y * filled.width) + x] = horizontal * verticalTop * verticalBottom
        }
    }
    return alpha
}

private func rgbToHSV(_ pixel: RGBA) -> (Double, Double, Double) {
    let r = Double(pixel.r) / 255
    let g = Double(pixel.g) / 255
    let b = Double(pixel.b) / 255
    let maximum = max(r, max(g, b))
    let minimum = min(r, min(g, b))
    let delta = maximum - minimum
    var hue = 0.0
    if delta > 0.000_001 {
        if maximum == r {
            hue = ((g - b) / delta).truncatingRemainder(dividingBy: 6)
        } else if maximum == g {
            hue = ((b - r) / delta) + 2
        } else {
            hue = ((r - g) / delta) + 4
        }
        hue /= 6
        if hue < 0 { hue += 1 }
    }
    return (hue, maximum <= 0 ? 0 : delta / maximum, maximum)
}

private func hsvToRGB(h: Double, s: Double, v: Double) -> RGBA {
    let wrappedHue = h - floor(h)
    let chroma = v * s
    let sector = wrappedHue * 6
    let x = chroma * (1 - abs(sector.truncatingRemainder(dividingBy: 2) - 1))
    let m = v - chroma
    let raw: (Double, Double, Double)
    switch Int(floor(sector)) % 6 {
    case 0: raw = (chroma, x, 0)
    case 1: raw = (x, chroma, 0)
    case 2: raw = (0, chroma, x)
    case 3: raw = (0, x, chroma)
    case 4: raw = (x, 0, chroma)
    default: raw = (chroma, 0, x)
    }
    return RGBA(
        r: UInt8(clamp((raw.0 + m) * 255, 0, 255)),
        g: UInt8(clamp((raw.1 + m) * 255, 0, 255)),
        b: UInt8(clamp((raw.2 + m) * 255, 0, 255)),
        a: 255)
}

private func colorize(_ source: ImageBuffer, variant: ColorVariant) -> ImageBuffer {
    if variant.preserveGreen { return source }
    var output = source
    for y in 0..<source.height {
        for x in 0..<source.width {
            let pixel = source[x, y]
            let hsv = rgbToHSV(pixel)
            if hsv.1 < 0.10 || hsv.2 < 0.08 {
                continue
            }

            var hueDelta = hsv.0 - 0.285
            if hueDelta > 0.5 { hueDelta -= 1 }
            if hueDelta < -0.5 { hueDelta += 1 }
            output[x, y] = hsvToRGB(
                h: variant.targetHue + (hueDelta * 0.12),
                s: clamp(hsv.1 * variant.saturationScale),
                v: clamp(hsv.2 * variant.valueScale))
        }
    }
    return output
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

    func channel(_ a: UInt8, _ b: UInt8, _ c: UInt8, _ d: UInt8) -> UInt8 {
        let top = (Double(a) * (1 - tx)) + (Double(b) * tx)
        let bottom = (Double(c) * (1 - tx)) + (Double(d) * tx)
        return UInt8(clamp((top * (1 - ty)) + (bottom * ty), 0, 255))
    }

    let p00 = image[x0, y0]
    let p10 = image[x1, y0]
    let p01 = image[x0, y1]
    let p11 = image[x1, y1]
    return RGBA(
        r: channel(p00.r, p10.r, p01.r, p11.r),
        g: channel(p00.g, p10.g, p01.g, p11.g),
        b: channel(p00.b, p10.b, p01.b, p11.b),
        a: channel(p00.a, p10.a, p01.a, p11.a))
}

private func cropAndScale(
    _ source: ImageBuffer,
    silhouette: [Double]
) -> ImageBuffer {
    var output = ImageBuffer(width: outputWidth, height: outputHeight)
    for y in 0..<outputHeight {
        let sourceY = Double(cropY) +
            ((Double(y) + 0.5) * Double(cropHeight) / Double(outputHeight)) - 0.5
        for x in 0..<outputWidth {
            let sourceX = Double(cropX) +
                ((Double(x) + 0.5) * Double(cropWidth) / Double(outputWidth)) - 0.5
            let color = sample(source, x: sourceX, y: sourceY)
            let alphaX = min(source.width - 1, max(0, Int(round(sourceX))))
            let alphaY = min(source.height - 1, max(0, Int(round(sourceY))))
            let alpha = silhouette[(alphaY * source.width) + alphaX]
            output[x, y] = RGBA(
                r: UInt8(clamp(Double(color.r) * alpha, 0, 255)),
                g: UInt8(clamp(Double(color.g) * alpha, 0, 255)),
                b: UInt8(clamp(Double(color.b) * alpha, 0, 255)),
                a: UInt8(clamp(alpha * 255, 0, 255)))
        }
    }
    return output
}

private func makeSoftShadowLayer(from subject: ImageBuffer) -> ImageBuffer {
    let radius = 4
    let offsetX = 2
    let offsetY = 3
    var shadow = ImageBuffer(width: subject.width, height: subject.height)
    for y in 0..<subject.height {
        for x in 0..<subject.width {
            var alphaSum = 0.0
            var sampleCount = 0.0
            for sampleY in (y - offsetY - radius)...(y - offsetY + radius) {
                if sampleY < 0 || sampleY >= subject.height { continue }
                for sampleX in (x - offsetX - radius)...(x - offsetX + radius) {
                    if sampleX < 0 || sampleX >= subject.width { continue }
                    alphaSum += Double(subject[sampleX, sampleY].a) / 255
                    sampleCount += 1
                }
            }
            let shadowAlpha = clamp((alphaSum / max(1, sampleCount)) * 0.20)
            shadow[x, y] = RGBA(
                r: UInt8(18 * shadowAlpha),
                g: UInt8(24 * shadowAlpha),
                b: UInt8(48 * shadowAlpha),
                a: UInt8(255 * shadowAlpha))
        }
    }
    return shadow
}

private func addSoftShadow(
    to subject: ImageBuffer,
    shadow: ImageBuffer
) -> ImageBuffer {
    var output = subject
    for y in 0..<subject.height {
        for x in 0..<subject.width {
            output[x, y] = composite(subject[x, y], over: shadow[x, y])
        }
    }
    return output
}

// Builds a distance field from transparency that is connected to the canvas
// edge. Internal molded details are intentionally ignored: the premium outline
// belongs only to the tray silhouette and must never enter the packed balls.
private func outsideDistanceField(from source: ImageBuffer) -> [Double] {
    let pixelCount = source.width * source.height
    var outside = [UInt8](repeating: 0, count: pixelCount)
    var queue: [Int] = []

    func appendIfTransparent(_ x: Int, _ y: Int) {
        let index = (y * source.width) + x
        if outside[index] == 0 && source[x, y].a <= 8 {
            outside[index] = 1
            queue.append(index)
        }
    }

    for x in 0..<source.width {
        appendIfTransparent(x, 0)
        appendIfTransparent(x, source.height - 1)
    }
    for y in 0..<source.height {
        appendIfTransparent(0, y)
        appendIfTransparent(source.width - 1, y)
    }

    var cursor = 0
    while cursor < queue.count {
        let index = queue[cursor]
        cursor += 1
        let x = index % source.width
        let y = index / source.width
        let neighbors = [
            x > 0 ? index - 1 : -1,
            x + 1 < source.width ? index + 1 : -1,
            y > 0 ? index - source.width : -1,
            y + 1 < source.height ? index + source.width : -1
        ]
        for next in neighbors where next >= 0 && outside[next] == 0 {
            let pixel = next * 4
            if source.pixels[pixel + 3] <= 8 {
                outside[next] = 1
                queue.append(next)
            }
        }
    }

    let diagonal = 1.414_213_562_37
    var distance = outside.map { $0 == 1 ? 0.0 : Double.greatestFiniteMagnitude }
    for y in 0..<source.height {
        for x in 0..<source.width {
            let index = (y * source.width) + x
            var value = distance[index]
            if x > 0 { value = min(value, distance[index - 1] + 1) }
            if y > 0 { value = min(value, distance[index - source.width] + 1) }
            if x > 0 && y > 0 {
                value = min(value, distance[index - source.width - 1] + diagonal)
            }
            if x + 1 < source.width && y > 0 {
                value = min(value, distance[index - source.width + 1] + diagonal)
            }
            distance[index] = value
        }
    }
    for y in stride(from: source.height - 1, through: 0, by: -1) {
        for x in stride(from: source.width - 1, through: 0, by: -1) {
            let index = (y * source.width) + x
            var value = distance[index]
            if x + 1 < source.width { value = min(value, distance[index + 1] + 1) }
            if y + 1 < source.height {
                value = min(value, distance[index + source.width] + 1)
            }
            if x + 1 < source.width && y + 1 < source.height {
                value = min(value, distance[index + source.width + 1] + diagonal)
            }
            if x > 0 && y + 1 < source.height {
                value = min(value, distance[index + source.width - 1] + diagonal)
            }
            distance[index] = value
        }
    }
    return distance
}

private func addPremiumSilhouetteOutline(
    to subject: ImageBuffer,
    width: Double
) -> ImageBuffer {
    let distance = outsideDistanceField(from: subject)
    let outline = RGBA(r: 21, g: 25, b: 43, a: 255)
    var output = subject
    for y in 0..<subject.height {
        for x in 0..<subject.width {
            let sourcePixel = subject[x, y]
            if sourcePixel.a <= 8 { continue }

            let index = (y * subject.width) + x
            let coverage = 1 - smoothstep(width - 1.25, width + 0.75, distance[index])
            if coverage <= 0 { continue }

            let amount = coverage * 0.92
            let alpha = Double(sourcePixel.a) / 255
            let targetRed = Double(outline.r) * alpha
            let targetGreen = Double(outline.g) * alpha
            let targetBlue = Double(outline.b) * alpha
            output[x, y] = RGBA(
                r: UInt8(clamp((Double(sourcePixel.r) * (1 - amount)) + (targetRed * amount), 0, 255)),
                g: UInt8(clamp((Double(sourcePixel.g) * (1 - amount)) + (targetGreen * amount), 0, 255)),
                b: UInt8(clamp((Double(sourcePixel.b) * (1 - amount)) + (targetBlue * amount), 0, 255)),
                a: sourcePixel.a)
        }
    }
    return output
}

guard CommandLine.arguments.count == 4 else {
    fatalError(
        "Usage: bake_approved_top_trays.swift " +
        "<approved-filled-green.png> <approved-empty-green.png> <output-folder>")
}

let filledPath = CommandLine.arguments[1]
let emptyPath = CommandLine.arguments[2]
let outputFolder = CommandLine.arguments[3]
try FileManager.default.createDirectory(
    atPath: outputFolder,
    withIntermediateDirectories: true)

private let filled = ImageBuffer(image: loadImage(filledPath)).flippedVertically()
private let empty = ImageBuffer(image: loadImage(emptyPath)).flippedVertically()
guard filled.width == sourceSize && filled.height == sourceSize &&
      empty.width == sourceSize && empty.height == sourceSize else {
    fatalError("Approved tray sources must both be 1254 x 1254")
}

let silhouette = buildSilhouetteAlpha(from: filled)
private let occupancyFrames = (0...9).map {
    makeOccupancyFrame(filled: filled, empty: empty, remainingCount: $0)
}
private let shadowReference = cropAndScale(filled, silhouette: silhouette)
private let shadowLayer = makeSoftShadowLayer(from: shadowReference)
for variant in variants {
    for remainingCount in 0...9 {
        let occupancy = occupancyFrames[remainingCount]
        let colored = colorize(occupancy, variant: variant)
        let trimmed = cropAndScale(colored, silhouette: silhouette)
        let outlined = addPremiumSilhouetteOutline(to: trimmed, width: 14.0)
        let final = addSoftShadow(to: outlined, shadow: shadowLayer)
        let filename = String(
            format: "TopTray_%@_%02d.png",
            variant.name,
            remainingCount)
        writePNG(
            final.makeImage(),
            to: URL(fileURLWithPath: outputFolder)
                .appendingPathComponent(filename).path)
    }
}

print(
    "Baked 50 approved 3x3 tray occupancy frames at " +
    "\(outputWidth)x\(outputHeight), with one transparent pre-rendered layer per state.")

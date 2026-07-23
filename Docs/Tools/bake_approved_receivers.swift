import CoreGraphics
import Foundation
import ImageIO
import UniformTypeIdentifiers

// Converts the five approved blue receiver masters into complete, transparent
// runtime sprites for every supported colour. Each output is a single baked
// receiver image: Unity never reconstructs the wall, rim, wells, balls, lid,
// outline, lighting, or spacing from separate visual parts.

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

    init(width: Int, height: Int) {
        self.width = width
        self.height = height
        pixels = [UInt8](repeating: 0, count: width * height * 4)
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
                fatalError("Could not create receiver image context")
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
                bitmapInfo: CGBitmapInfo(rawValue: CGImageAlphaInfo.premultipliedLast.rawValue),
                provider: provider,
                decode: nil,
                shouldInterpolate: true,
                intent: .defaultIntent)
        else {
            fatalError("Could not create baked receiver image")
        }
        return image
    }

    func flippedVertically() -> ImageBuffer {
        var result = ImageBuffer(width: width, height: height)
        for y in 0..<height {
            for x in 0..<width {
                result[x, height - 1 - y] = self[x, y]
            }
        }
        return result
    }
}

private struct Bounds {
    var minX: Int
    var minY: Int
    var maxX: Int
    var maxY: Int

    var width: Int { maxX - minX + 1 }
    var height: Int { maxY - minY + 1 }
}

private struct PreparedSource {
    let image: ImageBuffer
    let alpha: [Double]
    let bounds: Bounds
}

private struct ColorVariant {
    let name: String
    let hue: Double
    let saturationScale: Double
    let valueScale: Double
    let preserveBlue: Bool
}

private let variants = [
    ColorVariant(name: "Green", hue: 0.285, saturationScale: 1.04, valueScale: 1.00, preserveBlue: false),
    ColorVariant(name: "Blue", hue: 0.625, saturationScale: 1.00, valueScale: 1.00, preserveBlue: true),
    ColorVariant(name: "Orange", hue: 0.070, saturationScale: 1.10, valueScale: 1.06, preserveBlue: false),
    ColorVariant(name: "Yellow", hue: 0.145, saturationScale: 1.02, valueScale: 1.12, preserveBlue: false),
    ColorVariant(name: "Pink", hue: 0.925, saturationScale: 1.08, valueScale: 1.06, preserveBlue: false)
]

private let outputWidth = 768
private let outputHeight = 416
private let contentX = 12.0
private let contentY = 12.0
private let contentWidth = 744.0
private let contentHeight = 392.0

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
        fatalError("Could not load approved receiver source: \(path)")
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
        fatalError("Could not create receiver PNG destination")
    }
    CGImageDestinationAddImage(destination, image, nil)
    guard CGImageDestinationFinalize(destination) else {
        fatalError("Could not write receiver PNG: \(path)")
    }
}

private func rgbToHSV(_ pixel: RGBA) -> (h: Double, s: Double, v: Double) {
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

private func hsvToRGB(h: Double, s: Double, v: Double, alpha: UInt8 = 255) -> RGBA {
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
        a: alpha)
}

private func isReceiverPixel(_ pixel: RGBA) -> Bool {
    let hsv = rgbToHSV(pixel)
    return hsv.s > 0.23 && hsv.v > 0.07 && hsv.h > 0.50 && hsv.h < 0.75
}

private func prepare(_ image: ImageBuffer) -> PreparedSource {
    let pixelCount = image.width * image.height
    var detected = [UInt8](repeating: 0, count: pixelCount)
    for y in 0..<image.height {
        for x in 0..<image.width where isReceiverPixel(image[x, y]) {
            detected[(y * image.width) + x] = 1
        }
    }

    // The generated studio background can contain small blue-tinted patches.
    // Isolate only the largest connected blue component so no detached strip,
    // shadow, or background pixel can enter the transparent runtime asset.
    var visited = [UInt8](repeating: 0, count: pixelCount)
    var largestComponent: [Int] = []
    for start in 0..<pixelCount where detected[start] == 1 && visited[start] == 0 {
        var component: [Int] = []
        var queue: [Int] = [start]
        visited[start] = 1
        var cursor = 0
        while cursor < queue.count {
            let index = queue[cursor]
            cursor += 1
            component.append(index)
            let x = index % image.width
            let y = index / image.width
            if x > 0 {
                let next = index - 1
                if detected[next] == 1 && visited[next] == 0 {
                    visited[next] = 1
                    queue.append(next)
                }
            }
            if x + 1 < image.width {
                let next = index + 1
                if detected[next] == 1 && visited[next] == 0 {
                    visited[next] = 1
                    queue.append(next)
                }
            }
            if y > 0 {
                let next = index - image.width
                if detected[next] == 1 && visited[next] == 0 {
                    visited[next] = 1
                    queue.append(next)
                }
            }
            if y + 1 < image.height {
                let next = index + image.width
                if detected[next] == 1 && visited[next] == 0 {
                    visited[next] = 1
                    queue.append(next)
                }
            }
        }
        if component.count > largestComponent.count {
            largestComponent = component
        }
    }

    var rowMinimum = [Int](repeating: Int.max, count: image.height)
    var rowMaximum = [Int](repeating: Int.min, count: image.height)
    var bounds = Bounds(minX: image.width, minY: image.height, maxX: -1, maxY: -1)

    for index in largestComponent {
        let x = index % image.width
        let y = index / image.width
        rowMinimum[y] = min(rowMinimum[y], x)
        rowMaximum[y] = max(rowMaximum[y], x)
        bounds.minX = min(bounds.minX, x)
        bounds.minY = min(bounds.minY, y)
        bounds.maxX = max(bounds.maxX, x)
        bounds.maxY = max(bounds.maxY, y)
    }

    guard bounds.maxX >= bounds.minX && bounds.maxY >= bounds.minY else {
        fatalError("Could not isolate the approved blue receiver")
    }

    // The saturated blue subject detection already reaches the polished outer
    // rim. Keep only one antialiasing pixel around it: a wider expansion would
    // bake the pale studio background or contact shadow into the runtime sprite.
    bounds.minX = max(0, bounds.minX - 1)
    bounds.minY = max(0, bounds.minY - 1)
    bounds.maxX = min(image.width - 1, bounds.maxX + 1)
    bounds.maxY = min(image.height - 1, bounds.maxY + 1)

    var alpha = [Double](repeating: 0, count: image.width * image.height)
    for y in bounds.minY...bounds.maxY {
        var sampleY = min(max(y, 0), image.height - 1)
        if rowMinimum[sampleY] == Int.max {
            var distance = 1
            while distance < 16 && rowMinimum[sampleY] == Int.max {
                let above = max(0, y - distance)
                let below = min(image.height - 1, y + distance)
                if rowMinimum[above] != Int.max {
                    sampleY = above
                } else if rowMinimum[below] != Int.max {
                    sampleY = below
                }
                distance += 1
            }
        }
        if rowMinimum[sampleY] == Int.max { continue }

        let left = Double(max(bounds.minX, rowMinimum[sampleY] - 1))
        let right = Double(min(bounds.maxX, rowMaximum[sampleY] + 1))
        for x in max(bounds.minX, Int(floor(left)) - 2)...min(bounds.maxX, Int(ceil(right)) + 2) {
            let horizontal = smoothstep(left - 1.25, left + 0.75, Double(x)) *
                (1 - smoothstep(right - 0.75, right + 1.25, Double(x)))
            let vertical = smoothstep(Double(bounds.minY) - 0.5, Double(bounds.minY) + 1.5, Double(y)) *
                (1 - smoothstep(Double(bounds.maxY) - 1.5, Double(bounds.maxY) + 0.5, Double(y)))
            alpha[(y * image.width) + x] = horizontal * vertical
        }
    }

    return PreparedSource(image: image, alpha: alpha, bounds: bounds)
}

private func samplePixel(_ image: ImageBuffer, x: Double, y: Double) -> RGBA {
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
        a: 255)
}

private func sampleAlpha(_ source: PreparedSource, x: Double, y: Double) -> Double {
    let safeX = clamp(x, 0, Double(source.image.width - 1))
    let safeY = clamp(y, 0, Double(source.image.height - 1))
    let x0 = Int(floor(safeX))
    let y0 = Int(floor(safeY))
    let x1 = min(x0 + 1, source.image.width - 1)
    let y1 = min(y0 + 1, source.image.height - 1)
    let tx = safeX - Double(x0)
    let ty = safeY - Double(y0)
    let a00 = source.alpha[(y0 * source.image.width) + x0]
    let a10 = source.alpha[(y0 * source.image.width) + x1]
    let a01 = source.alpha[(y1 * source.image.width) + x0]
    let a11 = source.alpha[(y1 * source.image.width) + x1]
    let top = (a00 * (1 - tx)) + (a10 * tx)
    let bottom = (a01 * (1 - tx)) + (a11 * tx)
    return clamp((top * (1 - ty)) + (bottom * ty))
}

private func recolor(_ pixel: RGBA, for variant: ColorVariant) -> RGBA {
    if variant.preserveBlue { return pixel }
    let hsv = rgbToHSV(pixel)
    if hsv.s < 0.08 || hsv.v < 0.055 { return pixel }

    var sourceHueOffset = hsv.h - 0.625
    if sourceHueOffset > 0.5 { sourceHueOffset -= 1 }
    if sourceHueOffset < -0.5 { sourceHueOffset += 1 }
    return hsvToRGB(
        h: variant.hue + (sourceHueOffset * 0.10),
        s: clamp(hsv.s * variant.saturationScale),
        v: clamp(hsv.v * variant.valueScale),
        alpha: pixel.a)
}

private func bake(_ source: PreparedSource, variant: ColorVariant) -> ImageBuffer {
    var output = ImageBuffer(width: outputWidth, height: outputHeight)
    for y in 0..<outputHeight {
        let normalizedY = (Double(y) + 0.5 - contentY) / contentHeight
        if normalizedY < 0 || normalizedY > 1 { continue }
        let sourceY = Double(source.bounds.minY) +
            (normalizedY * Double(source.bounds.height)) - 0.5
        for x in 0..<outputWidth {
            let normalizedX = (Double(x) + 0.5 - contentX) / contentWidth
            if normalizedX < 0 || normalizedX > 1 { continue }
            let sourceX = Double(source.bounds.minX) +
                (normalizedX * Double(source.bounds.width)) - 0.5
            var alpha = sampleAlpha(source, x: sourceX, y: sourceY)
            if alpha <= 0 { continue }
            let sampled = samplePixel(source.image, x: sourceX, y: sourceY)
            let sampledHSV = rgbToHSV(sampled)
            if sampledHSV.s < 0.18 && (normalizedY < 0.08 || normalizedY > 0.965) {
                alpha = 0
            }
            if alpha <= 0 { continue }
            let recolored = recolor(sampled, for: variant)
            output[x, y] = RGBA(
                r: UInt8(clamp(Double(recolored.r) * alpha, 0, 255)),
                g: UInt8(clamp(Double(recolored.g) * alpha, 0, 255)),
                b: UInt8(clamp(Double(recolored.b) * alpha, 0, 255)),
                a: UInt8(clamp(alpha * 255, 0, 255)))
        }
    }
    let isolated = keepLargestAlphaComponent(output)
    return addPremiumSilhouetteOutline(to: isolated, width: 8.0)
}

private func keepLargestAlphaComponent(_ source: ImageBuffer) -> ImageBuffer {
    let pixelCount = source.width * source.height
    var visited = [UInt8](repeating: 0, count: pixelCount)
    var largest: [Int] = []
    for start in 0..<pixelCount where source.pixels[(start * 4) + 3] > 8 && visited[start] == 0 {
        var component: [Int] = []
        var queue: [Int] = [start]
        visited[start] = 1
        var cursor = 0
        while cursor < queue.count {
            let index = queue[cursor]
            cursor += 1
            component.append(index)
            let x = index % source.width
            let y = index / source.width
            let neighbors = [
                x > 0 ? index - 1 : -1,
                x + 1 < source.width ? index + 1 : -1,
                y > 0 ? index - source.width : -1,
                y + 1 < source.height ? index + source.width : -1
            ]
            for next in neighbors where next >= 0 &&
                source.pixels[(next * 4) + 3] > 8 && visited[next] == 0 {
                visited[next] = 1
                queue.append(next)
            }
        }
        if component.count > largest.count { largest = component }
    }

    var keep = [UInt8](repeating: 0, count: pixelCount)
    for index in largest { keep[index] = 1 }
    var output = source
    for index in 0..<pixelCount where keep[index] == 0 {
        let pixel = index * 4
        output.pixels[pixel] = 0
        output.pixels[pixel + 1] = 0
        output.pixels[pixel + 2] = 0
        output.pixels[pixel + 3] = 0
    }
    return output
}

// Only transparent pixels connected to the canvas edge define the receiver's
// silhouette. This keeps the outline outside the wells and packed balls.
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
            let coverage = 1 - smoothstep(width - 1.0, width + 0.75, distance[index])
            if coverage <= 0 { continue }

            let amount = coverage * 0.92
            let alpha = Double(sourcePixel.a) / 255
            output[x, y] = RGBA(
                r: UInt8(clamp((Double(sourcePixel.r) * (1 - amount)) + (Double(outline.r) * alpha * amount), 0, 255)),
                g: UInt8(clamp((Double(sourcePixel.g) * (1 - amount)) + (Double(outline.g) * alpha * amount), 0, 255)),
                b: UInt8(clamp((Double(sourcePixel.b) * (1 - amount)) + (Double(outline.b) * alpha * amount), 0, 255)),
                a: sourcePixel.a)
        }
    }
    return output
}

guard CommandLine.arguments.count == 7 else {
    fatalError(
        "Usage: bake_approved_receivers.swift <open00> <open01> <open02> " +
        "<open03> <closed> <output-folder>")
}

private let sourcePaths = Array(CommandLine.arguments[1...5])
private let outputFolder = CommandLine.arguments[6]
try FileManager.default.createDirectory(atPath: outputFolder, withIntermediateDirectories: true)

private let sources = sourcePaths.map {
    prepare(ImageBuffer(image: loadImage($0)).flippedVertically())
}

for variant in variants {
    for count in 0...3 {
        let filename = String(format: "Receiver_%@_Open_%02d.png", variant.name, count)
        writePNG(
            bake(sources[count], variant: variant).makeImage(),
            to: URL(fileURLWithPath: outputFolder).appendingPathComponent(filename).path)
    }
    writePNG(
        bake(sources[4], variant: variant).makeImage(),
        to: URL(fileURLWithPath: outputFolder)
            .appendingPathComponent("Receiver_\(variant.name)_Closed.png").path)
}

print(
    "Baked 25 complete approved receiver sprites at \(outputWidth)x\(outputHeight); " +
    "no visual receiver parts are reconstructed in Unity.")

import CoreGraphics
import Foundation
import ImageIO
import UniformTypeIdentifiers

// Bakes the approved standalone blue hidden tray into four pixel-identical
// color variants. The source geometry, highlight, bevel, front wall, outline,
// and perspective are preserved. Only the hue treatment changes.

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
                fatalError("Could not create source image context")
            }

            context.translateBy(x: 0, y: CGFloat(height))
            context.scaleBy(x: 1, y: -1)
            context.draw(image, in: CGRect(x: 0, y: 0, width: width, height: height))
        }
    }

    subscript(x: Int, y: Int) -> RGBA {
        get {
            let offset = ((y * width) + x) * 4
            return RGBA(
                r: pixels[offset],
                g: pixels[offset + 1],
                b: pixels[offset + 2],
                a: pixels[offset + 3])
        }
        set {
            let offset = ((y * width) + x) * 4
            pixels[offset] = newValue.r
            pixels[offset + 1] = newValue.g
            pixels[offset + 2] = newValue.b
            pixels[offset + 3] = newValue.a
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
            fatalError("Could not create output image")
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
    let preservesBlue: Bool
}

private let variants = [
    ColorVariant(
        name: "Green",
        targetHue: 0.285,
        saturationScale: 0.91,
        valueScale: 1.02,
        preservesBlue: false),
    ColorVariant(
        name: "Blue",
        targetHue: 0.625,
        saturationScale: 1.0,
        valueScale: 1.0,
        preservesBlue: true),
    ColorVariant(
        name: "Orange",
        targetHue: 0.070,
        saturationScale: 1.03,
        valueScale: 1.08,
        preservesBlue: false),
    ColorVariant(
        name: "Yellow",
        targetHue: 0.145,
        saturationScale: 0.88,
        valueScale: 1.14,
        preservesBlue: false)
]

private let requiredSourceSize = 1254
private let outputWidth = 512
private let outputHeight = 466
private let cropPaddingX = 18
private let cropPaddingTop = 15
private let cropPaddingBottom = 25

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
        fatalError("Could not create \(path)")
    }
    CGImageDestinationAddImage(destination, image, nil)
    guard CGImageDestinationFinalize(destination) else {
        fatalError("Could not write \(path)")
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

private func circularHueDistance(_ first: Double, _ second: Double) -> Double {
    let difference = abs(first - second)
    return min(difference, 1 - difference)
}

private func isBlueTraySeed(_ pixel: RGBA) -> Bool {
    let hsv = rgbToHSV(pixel)
    return hsv.s >= 0.48 &&
        circularHueDistance(hsv.h, 0.625) <= 0.145 &&
        hsv.v >= 0.08
}

private struct Silhouette {
    let alpha: [Double]
    let firstRow: Int
    let lastRow: Int
    let firstColumn: Int
    let lastColumn: Int
}

private func buildSilhouette(from source: ImageBuffer) -> Silhouette {
    var rowMinimum = [Int](repeating: Int.max, count: source.height)
    var rowMaximum = [Int](repeating: Int.min, count: source.height)
    var rowCount = [Int](repeating: 0, count: source.height)

    for y in 0..<source.height {
        for x in 0..<source.width where isBlueTraySeed(source[x, y]) {
            rowMinimum[y] = min(rowMinimum[y], x)
            rowMaximum[y] = max(rowMaximum[y], x)
            rowCount[y] += 1
        }
    }

    let candidateRows = (0..<source.height).filter { rowCount[$0] >= 32 }
    guard let firstRow = candidateRows.first,
          let lastRow = candidateRows.last else {
        fatalError("Could not isolate the approved standalone blue tray")
    }

    func smoothedBoundary(at row: Int, minimum: Bool) -> Double? {
        var samples: [Int] = []
        for sampleRow in max(firstRow, row - 4)...min(lastRow, row + 4) {
            let value = minimum ? rowMinimum[sampleRow] : rowMaximum[sampleRow]
            if value != Int.max && value != Int.min {
                samples.append(value)
            }
        }
        guard !samples.isEmpty else { return nil }
        samples.sort()
        return Double(samples[samples.count / 2])
    }

    var alpha = [Double](repeating: 0, count: source.width * source.height)
    var firstColumn = source.width
    var lastColumn = -1
    for y in firstRow...lastRow {
        guard let rawLeft = smoothedBoundary(at: y, minimum: true),
              let rawRight = smoothedBoundary(at: y, minimum: false) else {
            continue
        }

        // The saturated-blue seed sits just inside the white highlight and dark
        // silhouette. Expand a few source pixels so those approved edge pixels
        // remain intact, then antialias only the final boundary.
        let left = max(0, rawLeft - 4.5)
        let right = min(Double(source.width - 1), rawRight + 4.5)
        firstColumn = min(firstColumn, Int(floor(left)))
        lastColumn = max(lastColumn, Int(ceil(right)))

        let vertical = smoothstep(Double(firstRow) - 1.5, Double(firstRow) + 1.0, Double(y)) *
            (1 - smoothstep(Double(lastRow) - 1.0, Double(lastRow) + 1.5, Double(y)))
        for x in max(0, Int(floor(left)) - 2)...min(source.width - 1, Int(ceil(right)) + 2) {
            let horizontal = smoothstep(left - 1.25, left + 0.75, Double(x)) *
                (1 - smoothstep(right - 0.75, right + 1.25, Double(x)))
            alpha[(y * source.width) + x] = horizontal * vertical
        }
    }

    guard firstColumn <= lastColumn else {
        fatalError("The approved hidden-tray silhouette was empty")
    }

    return Silhouette(
        alpha: alpha,
        firstRow: firstRow,
        lastRow: lastRow,
        firstColumn: firstColumn,
        lastColumn: lastColumn)
}

private func colorize(_ source: ImageBuffer, variant: ColorVariant) -> ImageBuffer {
    if variant.preservesBlue { return source }
    var output = source
    for y in 0..<source.height {
        for x in 0..<source.width {
            let pixel = source[x, y]
            let hsv = rgbToHSV(pixel)
            guard hsv.s >= 0.10,
                  circularHueDistance(hsv.h, 0.625) <= 0.19 else {
                continue
            }

            var hueDelta = hsv.h - 0.625
            if hueDelta > 0.5 { hueDelta -= 1 }
            if hueDelta < -0.5 { hueDelta += 1 }
            output[x, y] = hsvToRGB(
                h: variant.targetHue + (hueDelta * 0.10),
                s: clamp(hsv.s * variant.saturationScale),
                v: clamp(hsv.v * variant.valueScale))
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

private func sampleAlpha(
    _ alpha: [Double],
    width: Int,
    height: Int,
    x: Double,
    y: Double
) -> Double {
    let safeX = clamp(x, 0, Double(width - 1))
    let safeY = clamp(y, 0, Double(height - 1))
    let x0 = Int(floor(safeX))
    let y0 = Int(floor(safeY))
    let x1 = min(x0 + 1, width - 1)
    let y1 = min(y0 + 1, height - 1)
    let tx = safeX - Double(x0)
    let ty = safeY - Double(y0)
    let top = (alpha[(y0 * width) + x0] * (1 - tx)) +
        (alpha[(y0 * width) + x1] * tx)
    let bottom = (alpha[(y1 * width) + x0] * (1 - tx)) +
        (alpha[(y1 * width) + x1] * tx)
    return (top * (1 - ty)) + (bottom * ty)
}

private func cropAndScale(
    _ source: ImageBuffer,
    silhouette: Silhouette
) -> ImageBuffer {
    let cropX = max(0, silhouette.firstColumn - cropPaddingX)
    let cropY = max(0, silhouette.firstRow - cropPaddingTop)
    let cropWidth = min(source.width - cropX,
                        (silhouette.lastColumn - silhouette.firstColumn + 1) +
                        (cropPaddingX * 2))
    let cropHeight = min(source.height - cropY,
                         (silhouette.lastRow - silhouette.firstRow + 1) +
                         cropPaddingTop + cropPaddingBottom)

    var output = ImageBuffer(width: outputWidth, height: outputHeight)
    for y in 0..<outputHeight {
        let sourceY = Double(cropY) +
            ((Double(y) + 0.5) * Double(cropHeight) / Double(outputHeight)) - 0.5
        for x in 0..<outputWidth {
            let sourceX = Double(cropX) +
                ((Double(x) + 0.5) * Double(cropWidth) / Double(outputWidth)) - 0.5
            let color = sample(source, x: sourceX, y: sourceY)
            let coverage = sampleAlpha(
                silhouette.alpha,
                width: source.width,
                height: source.height,
                x: sourceX,
                y: sourceY)
            output[x, y] = RGBA(
                r: UInt8(clamp(Double(color.r) * coverage, 0, 255)),
                g: UInt8(clamp(Double(color.g) * coverage, 0, 255)),
                b: UInt8(clamp(Double(color.b) * coverage, 0, 255)),
                a: UInt8(clamp(coverage * 255, 0, 255)))
        }
    }
    return output
}

private func composite(_ top: RGBA, over bottom: RGBA) -> RGBA {
    let topAlpha = Double(top.a) / 255
    let inverse = 1 - topAlpha
    return RGBA(
        r: UInt8(clamp(Double(top.r) + (Double(bottom.r) * inverse), 0, 255)),
        g: UInt8(clamp(Double(top.g) + (Double(bottom.g) * inverse), 0, 255)),
        b: UInt8(clamp(Double(top.b) + (Double(bottom.b) * inverse), 0, 255)),
        a: UInt8(clamp(
            (topAlpha + ((Double(bottom.a) / 255) * inverse)) * 255,
            0,
            255)))
}

private func addTightContactShadow(to subject: ImageBuffer) -> ImageBuffer {
    let radius = 3
    let offsetX = 1
    let offsetY = 4
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
            let shadowAlpha = clamp((alphaSum / max(1, sampleCount)) * 0.17)
            shadow[x, y] = RGBA(
                r: UInt8(12 * shadowAlpha),
                g: UInt8(20 * shadowAlpha),
                b: UInt8(43 * shadowAlpha),
                a: UInt8(255 * shadowAlpha))
        }
    }

    var output = subject
    for y in 0..<subject.height {
        for x in 0..<subject.width {
            output[x, y] = composite(subject[x, y], over: shadow[x, y])
        }
    }
    return output
}

guard CommandLine.arguments.count == 3 else {
    fatalError(
        "Usage: bake_approved_hidden_top_trays.swift " +
        "<approved-standalone-blue.png> <output-directory>")
}

private let sourcePath = CommandLine.arguments[1]
private let outputDirectory = CommandLine.arguments[2]
try FileManager.default.createDirectory(
    atPath: outputDirectory,
    withIntermediateDirectories: true)

private let source = ImageBuffer(image: loadImage(sourcePath)).flippedVertically()
guard source.width == requiredSourceSize && source.height == requiredSourceSize else {
    fatalError("Approved standalone hidden tray source must be 1254 x 1254")
}

private let silhouette = buildSilhouette(from: source)
print(
    "Detected approved tray bounds x=\(silhouette.firstColumn)...\(silhouette.lastColumn), " +
    "y=\(silhouette.firstRow)...\(silhouette.lastRow)")

for variant in variants {
    let colored = colorize(source, variant: variant)
    let cropped = cropAndScale(colored, silhouette: silhouette)
    let final = addTightContactShadow(to: cropped)
    let path = "\(outputDirectory)/HiddenTopTray_\(variant.name).png"
    writePNG(final.makeImage(), to: path)
    print("Wrote \(path)")
}

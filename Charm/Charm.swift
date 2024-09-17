//
//  Charm.swift
//  Charm
//
//  Created by Milch on 2024/01/01.
//

import WidgetKit
import SwiftUI

struct Provider: TimelineProvider {
    func placeholder(in context: Context) -> CharmEntry {
        CharmEntry(date: Date(), likes: 0)
    }

    func getSnapshot(in context: Context, completion: @escaping (CharmEntry) -> ()) {
        completion(CharmEntry(date: Date(), likes: 0))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<Entry>) -> ()) {
        Task {
            let applicationGroupIdentifier = "group.com.milchchan.Apricot"
            let currentDate = Date()
            let likes: Int
            var image: Image? = nil
            
            if let userDefaults = UserDefaults(suiteName: applicationGroupIdentifier) {
                likes = userDefaults.integer(forKey: "likes")
            } else {
                likes = 0
            }
            
            if let containerUrl = FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: applicationGroupIdentifier), let urls = try? FileManager.default.contentsOfDirectory(at: containerUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
                var imagePaths = [String]()
                
                for url in urls {
                    if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), let isDirectory = values.isDirectory, !isDirectory, let name = values.name, UUID(uuidString: name) != nil {
                        imagePaths.append(url.path(percentEncoded: false))
                    }
                }
                
                if !imagePaths.isEmpty {
                    let path = imagePaths[Int.random(in: 0..<imagePaths.count)]
                    let cgImage: CGImage? = await Task.detached {
                        if let file = FileHandle(forReadingAtPath: path) {
                            defer {
                                try? file.close()
                            }
                            
                            if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                for i in 0..<CGImageSourceGetCount(imageSource) {
                                    if let cgImage = CGImageSourceCreateImageAtIndex(imageSource, i, nil) {
                                        let limit = 512.0
                                        let imageWidth = Double(cgImage.width)
                                        let imageHeight = Double(cgImage.height)
                                        var resizeSize: CGSize? = nil
                                        
                                        if imageWidth < imageHeight {
                                            if imageHeight > limit {
                                                resizeSize = CGSize(width: floor(limit / imageHeight * imageWidth), height: limit)
                                            }
                                        } else if imageWidth > limit {
                                            resizeSize = CGSize(width: limit, height: floor(limit / imageWidth * imageHeight))
                                        }
                                        
                                        if let resizeSize {
                                            var resizedImage: CGImage? = nil
                                            
                                            UIGraphicsBeginImageContextWithOptions(CGSize(width: resizeSize.width, height: resizeSize.height), false, 1)
                                            
                                            if let context = UIGraphicsGetCurrentContext() {
                                                context.interpolationQuality = .high
                                                context.setAllowsAntialiasing(true)
                                                context.clear(CGRect(x: 0.0, y: 0.0, width: resizeSize.width, height: resizeSize.height))
                                                context.translateBy(x: 0.0, y: resizeSize.height)
                                                context.scaleBy(x: 1.0, y: -1.0)
                                                context.draw(cgImage, in: CGRect(x: 0.0, y: 0.0, width: resizeSize.width, height: resizeSize.height))
                                                resizedImage = context.makeImage()
                                            }
                                            
                                            UIGraphicsEndImageContext()
                                            
                                            return resizedImage
                                        } else {
                                            return cgImage
                                        }
                                    }
                                    
                                    break
                                }
                            }
                        }
                        
                        return nil
                    }.value
                    
                    if let cgImage {
                        image = Image(uiImage: UIImage(cgImage: cgImage))
                    }
                }
            }
            
            completion(Timeline(entries: [CharmEntry(date: currentDate, likes: likes, image: image)], policy: .after(Calendar.current.date(byAdding: DateComponents(minute: 30), to: currentDate)!)))
        }
    }
}

struct CharmEntry: TimelineEntry {
    let date: Date
    let likes: Int
    let image: Image
    let offset: Double
    
    init(date: Date, likes: Int, image: Image? = nil) {
        self.date = date
        
        self.likes = likes
        
        if let image {
            self.image = image
            self.offset = 0.0
        } else {
            self.image = Image("Milch", bundle: Bundle(identifier: "com.milchchan.Apricot.Charm"))
            self.offset = 0.5
        }
    }
}

struct CharmEntryView : View {
    @Environment(\.widgetFamily) var widgetFamily
    var entry: Provider.Entry
    private let starImage: UIImage
    
    var body: some View {
        switch self.widgetFamily {
        case .accessoryCircular:
            VStack(alignment: .center, spacing: 8.0) {
                Image(uiImage: self.starImage.withRenderingMode(.alwaysTemplate))
                    .frame(
                        width: self.starImage.size.width * 0.75,
                        height: self.starImage.size.height * 0.75,
                        alignment: .center
                    )
                    .foregroundStyle(.foreground)
                    .zIndex(1)
                    .scaleEffect(0.75)
                Text(String(format: "%ld", self.entry.likes))
                    .foregroundStyle(.foreground)
                    .font(.system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize * 1.5, weight: .bold))
                    .fontWeight(.heavy)
                    .lineLimit(1)
                    .truncationMode(.tail)
                    .frame(height: UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize * 1.5, weight: .bold).capHeight)
            }
            .containerBackground(.fill.tertiary, for: .widget)
        case .systemSmall:
            ZStack {
                GeometryReader { geometry in
                    self.entry.image
                        .resizable()
                        .scaledToFill()
                        .frame(
                            width: geometry.size.width,
                            height: geometry.size.height,
                            alignment: .top
                        )
                        .clipped()
                }
                .blur(radius: 32.0)
                GeometryReader { geometry in
                    self.entry.image
                        .resizable()
                        .scaledToFill()
                        .frame(
                            width: geometry.size.width,
                            height: geometry.size.height,
                            alignment: .top
                        )
                        .clipped()
                }
            }
        default:
            ZStack {
                GeometryReader { geometry in
                    self.entry.image
                        .resizable()
                        .scaledToFill()
                        .frame(
                            width: geometry.size.width,
                            height: geometry.size.height,
                            alignment: .top
                        )
                        .offset(y: self.widgetFamily == .systemMedium ? -geometry.size.height * self.entry.offset : 0.0)
                        .clipped()
                }
                .blur(radius: 32.0)
                GeometryReader { geometry in
                    self.entry.image
                        .resizable()
                        .scaledToFill()
                        .frame(
                            width: geometry.size.width,
                            height: geometry.size.height,
                            alignment: .top
                        )
                        .offset(y: self.widgetFamily == .systemMedium ? -geometry.size.height * self.entry.offset : 0.0)
                        .clipped()
                }
                VStack(alignment: .center, spacing: 8.0) {
                    Image(uiImage: self.starImage.withRenderingMode(.alwaysTemplate))
                        .frame(
                            width: self.starImage.size.width * 0.75,
                            height: self.starImage.size.height * 0.75,
                            alignment: .center
                        )
                        .foregroundColor(Color(uiColor: UIColor(named: "AccentColor")!))
                        .zIndex(1)
                        .scaleEffect(0.75)
                    Text(String(format: "%ld", self.entry.likes))
                        .foregroundColor(Color(uiColor: UIColor(named: "AccentColor")!))
                        .font(.custom("DIN2014-Bold", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 2.0)))
                        .lineLimit(1)
                        .truncationMode(.tail)
                        .frame(height: ceil(UIFont(name: "DIN2014-Bold", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 2.0))!.capHeight))
                        .contentTransition(.numericText(value: Double(self.entry.likes)))
                }
            }
            .containerBackground(for: .widget) {
                ContainerRelativeShape()
                    .foregroundColor(Color(uiColor: UIColor(named: "WidgetBackground")!))
            }
        }
    }
    
    init(entry: Provider.Entry) {
        let padding = 8.0
        var image = UIImage(named: "Star")!
        let maximum = max(image.size.width, image.size.height)
        let length = (maximum + padding * 2.0) * image.scale
        let scale = 0.75
        let size = CGSize(width: ceil(length * scale), height: ceil(length * scale))
        
        UIGraphicsBeginImageContextWithOptions(size, false, 1.0)
        
        if let context = UIGraphicsGetCurrentContext(), let starImage = image.cgImage {
            context.interpolationQuality = .high
            context.setAllowsAntialiasing(true)
            context.saveGState()
            context.clear(CGRect(x: 0.0, y: 0.0, width: size.width, height: size.height))
            context.translateBy(x: 0.0, y: size.height)
            context.scaleBy(x: scale, y: -scale)
            context.setFillColor(CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [1.0, 1.0, 1.0, 1.0])!)
            context.fill(CGRect(origin: CGPoint.zero, size: CGSize(width: length, height: length)))
            context.setFillColor(CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [0.0, 0.0, 0.0, 1.0])!)
            context.addArc(center: CGPoint(x: (padding + maximum / 2.0) * image.scale, y: (padding + maximum / 2.0) * image.scale), radius: (maximum + padding * 2.0) / 2.0 * image.scale, startAngle: 0.0, endAngle: Double.pi * 2.0, clockwise: true)
            context.fillPath()
            context.clip(to: CGRect(origin: CGPoint(x: (length - CGFloat(starImage.width)) / 2.0, y: (length - CGFloat(starImage.height)) / 2.0), size: CGSize(width: starImage.width, height: starImage.height)), mask: starImage)
            context.setFillColor(CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [1.0, 1.0, 1.0, 1.0])!)
            context.fill(CGRect(origin: CGPoint(x: padding * image.scale, y: padding * image.scale), size: CGSize(width: starImage.width, height: starImage.height)))
            context.restoreGState()
            
            if let cgImage = context.makeImage(), let dataProvider = cgImage.dataProvider, let maskImage = CGImage(maskWidth: cgImage.width, height: cgImage.height, bitsPerComponent: cgImage.bitsPerComponent, bitsPerPixel: cgImage.bitsPerPixel, bytesPerRow: cgImage.bytesPerRow, provider: dataProvider, decode: nil, shouldInterpolate: false), let maskedImage = cgImage.masking(maskImage) {
                context.clear(CGRect(x: 0.0, y: 0.0, width: size.width, height: size.height))
                context.translateBy(x: 0.0, y: size.height)
                context.scaleBy(x: 1.0, y: -1.0)
                context.draw(maskedImage, in: CGRect(x: 0, y: 0, width: size.width, height: size.height), byTiling: false)
                
                if let i = context.makeImage() {
                    image = UIImage(cgImage: i, scale: image.scale, orientation: image.imageOrientation)
                }
            }
        }
        
        UIGraphicsEndImageContext()
        
        self.entry = entry
        self.starImage = image
    }
}

struct Charm: Widget {
    let name: String = "Apricot"
    let kind: String = "Charm"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: "com.milchchan.\(self.name).\(self.kind)", provider: Provider()) { entry in
            CharmEntryView(entry: entry)
        }
        .configurationDisplayName(self.name)
        .description("Charm")
        .supportedFamilies([.systemSmall, .systemMedium, .systemLarge, .accessoryCircular])
        .contentMarginsDisabled()
    }
}

#Preview(as: .systemSmall) {
    Charm()
} timeline: {
    CharmEntry(date: .now, likes: 0)
}

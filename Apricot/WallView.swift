//
//  Wall.swift
//  Apricot
//
//  Created by Milch on 2023/01/01.
//

import Foundation
import CryptoKit
import CoreText
import UIKit

protocol WallDelegate: AnyObject {
    func wallCanSelect(_ wall: WallView, at index: Int) -> Bool
    func wallDidSelect(_ wall: WallView, at index: Int)
}

class WallView: UIView {
    weak var delegate: (any WallDelegate)? = nil
    private var tracker: (active: Bool, edge: Bool, movement: (x: Double, y: Double), velocity: (x: Double, y: Double)) = (active: false, edge: true, movement: (x: 0.0, y: 0.0), velocity: (x: 0.0, y: 0.0))
    private var pinches: [(touches: [UITouch], center: (x: Double, y: Double), movement: (x: Double, y: Double), radius: Double, velocity: Double, current:(x: Double, y: Double, radius: Double))] = []
    private var touches: [(touch: UITouch, location: (x: Double, y: Double), movement: (x: Double, y: Double), velocity: (x: Double, y: Double), timestamp: CFTimeInterval)] = []
    private var blocks: [(running: Bool, time: Double, duration: Double, type: (elapsed: Double, speed: Double, reverse: Bool, buffer: String, count: Int), text: String, attributes: [(start: Int, end: Int)], current: String, safe: [(text: String, framesetter: CTFramesetter?, size: CGSize)], scroll: (touch: UITouch?, step: Double), shake: (time: Double?, x: Double), elapsed: Double, rtl: Bool)] = []
    private var lines: [(text: String, attributes: [(start: Int, end: Int)])] = []
    private var isInvalidated = false
    private var isReloading = false
    private var isLoading = false
    private var isFetched = false
    private var time = 0.0
    private var revealStep: Double = 0.0
    private var loadingStep: Double = -1.0
    private var fetchedFrames: [(image: CGImage?, delay: Double)]? = nil
    private var backgroundFrames: [(image: CGImage?, delay: Double)]? = nil
    private var fetchedImage: CGImage? = nil
    private var blindColor: CGColor? = nil
    private var currentTime: Double? = nil
    private var sourceRect = CGRect.zero
    private var maskLayer: CAShapeLayer? = nil
    private var loadingLayer: CALayer? = nil
    private var blindLayer: CALayer? = nil
    private var imageLayer: CALayer? = nil
    private var backgroundLayer: CALayer? = nil
    private var particles: [(layer: CALayer?, time: Double, delay: Double, duration: Double)] = []
    private var requestParticles = 0
    private var fontCache: [String: CTFont] = [:]
    private var textCache: [String: (CTFramesetter, CGSize, CGPath?)] = [:]
    private let backgroundPattern = UIImage(named: "Stripes")!
    private let permutation = [151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180]
    var accentColor: CGColor? {
        get {
            return self.blindColor
        }
        set(color) {
            self.blindColor = color
        }
    }
    
    override init(frame: CGRect) {
        super.init(frame: frame)
        
        let visualEffectView = UIVisualEffectView(effect: UIBlurEffect(style: .systemUltraThinMaterial))
        let view = UIView()
        let maskSublayer = CAShapeLayer()
        let loadingLayer = CALayer()
        let blindLayer = CALayer()
        
        self.maskLayer = CAShapeLayer()
        self.maskLayer!.fillRule = .nonZero
        self.maskLayer!.strokeColor = UIColor.clear.cgColor
        self.maskLayer!.lineWidth = 0.0
        self.maskLayer!.fillColor = UIColor(white: 1.0, alpha: 1.0).cgColor
        
        maskSublayer.fillRule = .nonZero
        maskSublayer.strokeColor = UIColor.clear.cgColor
        maskSublayer.lineWidth = 0.0
        maskSublayer.fillColor = UIColor(white: 1.0, alpha: 1.0).cgColor
        
        self.maskLayer!.addSublayer(maskSublayer)
        
        loadingLayer.backgroundColor = UIColor(patternImage: self.backgroundPattern).cgColor
        loadingLayer.contentsGravity = .topLeft
        loadingLayer.transform = CATransform3DMakeScale(1.0, -1.0, 1.0)
        
        blindLayer.backgroundColor = UIColor(named: "AccentColor")!.cgColor
        blindLayer.contentsGravity = .resizeAspect
        blindLayer.masksToBounds = true
        
        if let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
            let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height)
            
            blindLayer.frame = CGRect(x: 0.0, y: 0.0, width: length, height: length)
            loadingLayer.frame = CGRect(x: 0.0, y: 0.0, width: length + self.backgroundPattern.size.width, height: length)
        }
        
        blindLayer.addSublayer(loadingLayer)
        
        self.blindColor = blindLayer.backgroundColor
        self.isUserInteractionEnabled = true
        self.isMultipleTouchEnabled = true
        self.backgroundColor = .clear
        self.contentMode = .center
        self.loadingLayer = loadingLayer
        self.blindLayer = blindLayer
        self.backgroundLayer = CALayer()
        self.backgroundLayer!.contentsGravity = .resizeAspect
        self.imageLayer = CALayer()
        self.imageLayer!.contentsGravity = .resizeAspect
        
        visualEffectView.translatesAutoresizingMaskIntoConstraints = false
        visualEffectView.isUserInteractionEnabled = false
        visualEffectView.backgroundColor = .clear
        visualEffectView.layer.addSublayer(self.backgroundLayer!)
        visualEffectView.contentView.isUserInteractionEnabled = false
        visualEffectView.contentView.layer.addSublayer(self.imageLayer!)
        
        self.addSubview(visualEffectView)
        
        view.translatesAutoresizingMaskIntoConstraints = false
        view.isUserInteractionEnabled = false
        view.backgroundColor = .clear
        view.layer.addSublayer(self.blindLayer!)
        
        self.addSubview(view)
        
        self.addConstraint(NSLayoutConstraint(item: visualEffectView, attribute: .leading, relatedBy: .equal, toItem: self, attribute: .leading, multiplier: 1.0, constant: 0.0))
        self.addConstraint(NSLayoutConstraint(item: visualEffectView, attribute: .top, relatedBy: .equal, toItem: self, attribute: .top, multiplier: 1.0, constant: 0.0))
        self.addConstraint(NSLayoutConstraint(item: visualEffectView, attribute: .trailing, relatedBy: .equal, toItem: self, attribute: .trailing, multiplier: 1.0, constant: 0.0))
        self.addConstraint(NSLayoutConstraint(item: visualEffectView, attribute: .bottom, relatedBy: .equal, toItem: self, attribute: .bottom, multiplier: 1.0, constant: 0.0))
        self.addConstraint(NSLayoutConstraint(item: view, attribute: .leading, relatedBy: .equal, toItem: self, attribute: .leading, multiplier: 1.0, constant: 0.0))
        self.addConstraint(NSLayoutConstraint(item: view, attribute: .top, relatedBy: .equal, toItem: self, attribute: .top, multiplier: 1.0, constant: 0.0))
        self.addConstraint(NSLayoutConstraint(item: view, attribute: .trailing, relatedBy: .equal, toItem: self, attribute: .trailing, multiplier: 1.0, constant: 0.0))
        self.addConstraint(NSLayoutConstraint(item: view, attribute: .bottom, relatedBy: .equal, toItem: self, attribute: .bottom, multiplier: 1.0, constant: 0.0))
        
        let displayLink = CADisplayLink(target: self, selector: #selector(self.update))
        
        displayLink.add(to: .current, forMode: .common)
    }
    
    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
    }
    
    func reload(lines: [(text: String, attributes: [(start: Int, end: Int)])]) {
        self.lines.removeAll()
        
        for line in lines {
            self.lines.append(line)
        }
        
        self.isInvalidated = true
    }
    
    func reload(frames: [[(url: URL?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)]], particles: Int) async {
        if self.backgroundFrames == nil && frames.isEmpty {
            self.emit(particles: particles)
        } else {
            self.isReloading = true
            self.isLoading = true
            self.isFetched = false
            
            Task {
                if let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                    let scale = Int(round(window.screen.scale))
                    let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height) * window.screen.scale
                    
                    self.fetchedFrames = await Task.detached {
                        var caches: [String: Data] = [:]
                        var minX = 0.0
                        var minY = 0.0
                        var maxWidth = 0.0
                        var maxHeight = 0.0
                        var maxDuration = 0.0
                        var animations: [[(image: CGImage?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)]] = []
                        
                        for animation in frames {
                            if !animation.isEmpty {
                                var tempFrames: [(image: CGImage?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)] = []
                                var duration = 0.0
                                
                                for frame in animation {
                                    if let url = frame.url {
                                        if url.scheme == "file" {
                                            var image: CGImage? = nil
                                            let width: Double
                                            let height: Double
                                            
                                            if let data = caches[url.absoluteString] {
                                                if let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                                    caches[url.absoluteString] = data
                                                    
                                                    for i in 0..<CGImageSourceGetCount(imageSource) {
                                                        image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                                        
                                                        break
                                                    }
                                                }
                                            } else {
                                                if scale > 1 {
                                                    let name = url.lastPathComponent[url.lastPathComponent.startIndex..<url.lastPathComponent.index(url.lastPathComponent.endIndex, offsetBy: -url.pathExtension.count - 1)]
                                                    let filename = "\(name)@\(scale)\(url.lastPathComponent[url.lastPathComponent.index(url.lastPathComponent.startIndex, offsetBy: name.count)..<url.lastPathComponent.endIndex])"
                                                    let path = url.deletingLastPathComponent().appending(path: filename, directoryHint: .inferFromPath).path(percentEncoded: false)
                                                    
                                                    if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                                        defer {
                                                            try? file.close()
                                                        }
                                                        
                                                        if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                                            caches[url.absoluteString] = data
                                                            
                                                            for i in 0..<CGImageSourceGetCount(imageSource) {
                                                                image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                                                
                                                                break
                                                            }
                                                        }
                                                    }
                                                }
                                                
                                                if image == nil {
                                                    let path = url.path(percentEncoded: false)
                                                    
                                                    if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                                        defer {
                                                            try? file.close()
                                                        }
                                                        
                                                        if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                                            caches[url.absoluteString] = data
                                                            
                                                            for i in 0..<CGImageSourceGetCount(imageSource) {
                                                                image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                                                
                                                                break
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            
                                            if let image {
                                                if frame.width == 0.0 && frame.height == 0.0 {
                                                    width = Double(image.width)
                                                    height = Double(image.height)
                                                } else if frame.width == 0.0 {
                                                    width = frame.height * Double(image.width) / Double(image.height)
                                                    height = frame.height
                                                } else if frame.height == 0.0 {
                                                    width = frame.width
                                                    height = frame.width * Double(image.height) / Double(image.width)
                                                } else {
                                                    width = frame.width
                                                    height = frame.height
                                                }
                                            } else {
                                                width = frame.width
                                                height = frame.height
                                            }
                                            
                                            if abs(frame.x) + width > maxWidth {
                                                maxWidth = abs(frame.x) + width
                                            }
                                            
                                            if abs(frame.y) + height > maxHeight {
                                                maxHeight = abs(frame.y) + height
                                            }
                                            
                                            tempFrames.append((image: image, x: frame.x, y: frame.y, width: width, height: height, opacity: frame.opacity, delay: frame.delay))
                                        } else if url.scheme == "https" {
                                            var imageSource: CGImageSource? = nil
                                            
                                            if let data = caches[url.absoluteString] {
                                                imageSource = CGImageSourceCreateWithData(data as CFData, nil)
                                            } else if let cachesUrl = FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask).first {
                                                let path = cachesUrl.appending(path: SHA256.hash(data: Data(url.absoluteString.utf8)).compactMap { String(format: "%02x", $0) }.joined(), directoryHint: .inferFromPath).path(percentEncoded: false)
                                                
                                                if FileManager.default.fileExists(atPath: path) {
                                                    if let file = FileHandle(forReadingAtPath: path) {
                                                        defer {
                                                            try? file.close()
                                                        }
                                                        
                                                        if let data = try? file.readToEnd() {
                                                            imageSource = CGImageSourceCreateWithData(data as CFData, nil)
                                                            caches[url.absoluteString] = data
                                                        }
                                                    }
                                                } else if let (data, response) = try? await URLSession.shared.data(for: URLRequest(url: url)), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "image/png" || httpResponse.mimeType == "image/apng" {
                                                    FileManager.default.createFile(atPath: path, contents: data, attributes: nil)
                                                    
                                                    imageSource = CGImageSourceCreateWithData(data as CFData, nil)
                                                    caches[url.absoluteString] = data
                                                }
                                            }
                                            
                                            if let imageSource {
                                                let through = tempFrames.count
                                                
                                                for i in 0..<CGImageSourceGetCount(imageSource) {
                                                    if let image = CGImageSourceCreateImageAtIndex(imageSource, i, nil) {
                                                        let width: Double
                                                        let height: Double
                                                        var delay = 0.0
                                                        
                                                        if frame.width == 0.0 && frame.height == 0.0 {
                                                            width = Double(image.width)
                                                            height = Double(image.height)
                                                        } else if frame.width == 0.0 {
                                                            width = frame.height * Double(image.width) / Double(image.height)
                                                            height = frame.height
                                                        } else if frame.height == 0.0 {
                                                            width = frame.width
                                                            height = frame.width * Double(image.height) / Double(image.width)
                                                        } else {
                                                            width = frame.width
                                                            height = frame.height
                                                        }
                                                        
                                                        if abs(frame.x) + width > maxWidth {
                                                            maxWidth = abs(frame.x) + width
                                                        }
                                                        
                                                        if abs(frame.y) + height > maxHeight {
                                                            maxHeight = abs(frame.y) + height
                                                        }
                                                        
                                                        if let properties = CGImageSourceCopyPropertiesAtIndex(imageSource, i, nil) as? [String: Any] {
                                                            for (key, value) in properties {
                                                                if key == kCGImagePropertyPNGDictionary as String, let dictionary = value as? [String: Any] {
                                                                    if let delayTime = dictionary[kCGImagePropertyAPNGUnclampedDelayTime as String] {
                                                                        if let number = delayTime as? NSNumber {
                                                                            let doubleValue = number.doubleValue
                                                                            
                                                                            if doubleValue <= 0.01 {
                                                                                delay = 0.1
                                                                            } else {
                                                                                delay = doubleValue
                                                                            }
                                                                        }
                                                                    } else if let delayTime = dictionary[kCGImagePropertyAPNGDelayTime as String] {
                                                                        if let number = delayTime as? NSNumber {
                                                                            let doubleValue = number.doubleValue
                                                                            
                                                                            if doubleValue <= 0.01 {
                                                                                delay = 0.1
                                                                            } else {
                                                                                delay = doubleValue
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        
                                                        tempFrames.append((image: image, x: frame.x, y: frame.y, width: width, height: height, opacity: frame.opacity, delay: delay))
                                                        duration += delay
                                                    }
                                                }
                                                
                                                if tempFrames.count > through {
                                                    for i in stride(from: tempFrames.count - 2, through: through, by: -1) {
                                                        tempFrames.append(tempFrames[i])
                                                        duration += tempFrames[i].delay
                                                    }
                                                    
                                                    tempFrames[tempFrames.count - 1].delay += frame.delay
                                                }
                                            }
                                        }
                                    } else {
                                        if abs(frame.x) + frame.width > maxWidth {
                                            maxWidth = abs(frame.x) + frame.width
                                        }
                                        
                                        if abs(frame.y) + frame.height > maxHeight {
                                            maxHeight = abs(frame.y) + frame.height
                                        }
                                        
                                        tempFrames.append((image: nil, x: frame.x, y: frame.y, width: frame.width, height: frame.height, opacity: frame.opacity, delay: frame.delay))
                                    }
                                    
                                    if frame.x < 0.0 && frame.x < minX {
                                        minX = frame.x
                                    }
                                    
                                    if frame.y < 0.0 && frame.y < minY {
                                        minY = frame.y
                                    }
                                    
                                    duration += frame.delay
                                }
                                
                                if duration > maxDuration {
                                    maxDuration = duration
                                }
                                
                                animations.append(tempFrames)
                            }
                        }
                        
                        if !animations.isEmpty {
                            var time = 0.0
                            var splittedAnimations: [(layers: [(image: CGImage?, x: Double, y: Double, width: Double, height: Double, opacity: Double)], delay: Double)] = []
                            var compositedFrames: [(image: CGImage?, delay: Double)] = []
                            let imageScale: Double
                            let width: Double
                            let height: Double
                            
                            if maxWidth < maxHeight {
                                if maxWidth > length {
                                    imageScale = length / maxWidth
                                    width = length
                                    height = floor(imageScale * maxHeight)
                                } else {
                                    imageScale = 1.0
                                    width = maxWidth
                                    height = maxHeight
                                }
                            } else if maxHeight > length {
                                imageScale = length / maxHeight
                                width = floor(imageScale * maxWidth)
                                height = length
                            } else {
                                imageScale = 1.0
                                width = maxWidth
                                height = maxHeight
                            }
                            
                            repeat {
                                var layers: [(image: CGImage?, x: Double, y: Double, width: Double, height: Double, opacity: Double)] = []
                                var minDelay: Double = Double.greatestFiniteMagnitude
                                
                                for animation in animations {
                                    var frame = animation[0]
                                    
                                    if frame.delay <= 0.01 {
                                        frame.delay = 0.1
                                    }
                                    
                                    var delay = frame.delay
                                    
                                    if time >= delay {
                                        for i in 1..<animation.count {
                                            frame = animation[i]
                                            delay += frame.delay
                                            
                                            if time < delay {
                                                break
                                            }
                                        }
                                    }
                                    
                                    layers.append((image: frame.image, x: frame.x, y: frame.y, width: frame.width, height: frame.height, opacity: frame.opacity))
                                    
                                    if delay - time > 0.0 && delay < minDelay {
                                        minDelay = delay
                                    }
                                }
                                
                                splittedAnimations.append((layers: layers, delay: minDelay - time))
                                time = minDelay
                            } while time < maxDuration
                            
                            for animation in splittedAnimations {
                                var compositedImage: CGImage? = nil
                                
                                UIGraphicsBeginImageContextWithOptions(CGSize(width: width, height: height), false, 1)
                                
                                if let context = UIGraphicsGetCurrentContext() {
                                    context.interpolationQuality = .high
                                    context.setAllowsAntialiasing(true)
                                    context.clear(CGRect(x: 0.0, y: 0.0, width: width, height: height))
                                    context.translateBy(x: 0.0, y: height)
                                    context.scaleBy(x: 1.0, y: -1.0)
                                    
                                    for layer in animation.layers {
                                        if let image = layer.image {
                                            context.draw(image, in: CGRect(x: round((layer.x - minX) * imageScale), y: round(height - (layer.y - minY + layer.height) * imageScale), width: floor(layer.width * imageScale), height: floor(layer.height * imageScale)))
                                        }
                                    }
                                    
                                    compositedImage = context.makeImage()
                                }
                                
                                UIGraphicsEndImageContext()
                                
                                compositedFrames.append((image: compositedImage, delay: animation.delay))
                            }
                            
                            return compositedFrames
                        }
                        
                        return  nil
                    }.value
                    self.requestParticles = particles
                }
                
                self.isReloading = false
            }
        }
    }
    
    func reload(image: CGImage) async {
        self.isReloading = true
        self.isLoading = true
        self.isFetched = false
        
        Task {
            if let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height)
                
                self.fetchedImage = await Task.detached {
                    var generatedImage: CGImage? = nil
                    
                    if let dataProvider = image.dataProvider {
                        var minX = Int.max
                        var minY = Int.max
                        var maxX = 0
                        var maxY = 0
                        let bytes: UnsafePointer = CFDataGetBytePtr(dataProvider.data)
                        let channels = image.bitsPerPixel / image.bitsPerComponent
                        let alphaInfo: CGImageAlphaInfo? = CGImageAlphaInfo(rawValue: image.bitmapInfo.rawValue & type(of: image.bitmapInfo).alphaInfoMask.rawValue)
                        let alphaFirst: Bool = alphaInfo == .premultipliedFirst || alphaInfo == .first || alphaInfo == .noneSkipFirst
                        let alphaLast: Bool = alphaInfo == .premultipliedLast || alphaInfo == .last || alphaInfo == .noneSkipLast
                        let littleEndian: Bool = image.bitmapInfo.contains(.byteOrder32Little)
                        var index: (alpha: Int, red: Int, green: Int, blue: Int)?
                        
                        if littleEndian {
                            if alphaFirst {
                                index = (alpha: 3, red: 2, green: 1, blue: 0)
                            } else if alphaLast {
                                index = (alpha: 0, red: 3, green: 2, blue: 1)
                            } else {
                                index = nil
                            }
                        } else if alphaFirst {
                            index = (alpha: 0, red: 1, green: 2, blue: 3)
                        } else if alphaLast {
                            index = (alpha: 3, red: 0, green: 1, blue: 2)
                        } else {
                            index = nil
                        }
                        
                        if let index {
                            for y in 0..<image.height {
                                for x in 0..<image.width {
                                    let offset = y * image.bytesPerRow + x * channels
                                    
                                    if bytes[offset + index.alpha] > 0 {
                                        if x < minX {
                                            minX = x
                                        }
                                        
                                        if y < minY {
                                            minY = y
                                        }
                                        
                                        if x > maxX {
                                            maxX = x
                                        }
                                        
                                        if y > maxY {
                                            maxY = y
                                        }
                                    }
                                }
                            }
                        }
                        
                        let cropWidth = maxX - minX
                        let cropHeight = maxY - minY
                        
                        if cropWidth > 0 && cropHeight > 0, let croppedImage = image.cropping(to: CGRect(x: minX, y: minY, width: cropWidth, height: cropHeight)) {
                            let imageWidth = Double(croppedImage.width)
                            let imageHeight = Double(croppedImage.height)
                            let limit = 64.0
                            let width: Double
                            let height: Double
                            var resizedImage: CGImage? = nil
                            
                            if imageWidth < imageHeight {
                                if imageHeight > limit {
                                    width = floor(limit / imageHeight * imageWidth)
                                    height = limit
                                } else {
                                    width = imageWidth
                                    height = imageHeight
                                }
                            } else if imageWidth > limit {
                                width = limit
                                height = floor(limit / imageWidth * imageHeight)
                            } else {
                                width = imageWidth
                                height = imageHeight
                            }
                            
                            UIGraphicsBeginImageContextWithOptions(CGSize(width: width, height: height), false, 1)
                            
                            if let context = UIGraphicsGetCurrentContext() {
                                context.interpolationQuality = .high
                                context.setAllowsAntialiasing(true)
                                context.clear(CGRect(x: 0.0, y: 0.0, width: width, height: height))
                                context.translateBy(x: 0.0, y: height)
                                context.scaleBy(x: 1.0, y: -1.0)
                                context.draw(croppedImage, in: CGRect(x: 0.0, y: 0.0, width: width, height: height))
                                resizedImage = context.makeImage()
                            }
                            
                            UIGraphicsEndImageContext()
                            
                            if let resizedImage, let dataProvider = resizedImage.dataProvider {
                                let bytes: UnsafePointer = CFDataGetBytePtr(dataProvider.data)
                                let channels = resizedImage.bitsPerPixel / resizedImage.bitsPerComponent
                                let alphaInfo: CGImageAlphaInfo? = CGImageAlphaInfo(rawValue: resizedImage.bitmapInfo.rawValue & type(of: resizedImage.bitmapInfo).alphaInfoMask.rawValue)
                                let alphaFirst: Bool = alphaInfo == .premultipliedFirst || alphaInfo == .first || alphaInfo == .noneSkipFirst
                                let alphaLast: Bool = alphaInfo == .premultipliedLast || alphaInfo == .last || alphaInfo == .noneSkipLast
                                let littleEndian: Bool = resizedImage.bitmapInfo.contains(.byteOrder32Little)
                                var index: (alpha: Int, red: Int, green: Int, blue: Int)?
                                
                                if littleEndian {
                                    if alphaFirst {
                                        index = (alpha: 3, red: 2, green: 1, blue: 0)
                                    } else if alphaLast {
                                        index = (alpha: 0, red: 3, green: 2, blue: 1)
                                    } else {
                                        index = nil
                                    }
                                } else if alphaFirst {
                                    index = (alpha: 0, red: 1, green: 2, blue: 3)
                                } else if alphaLast {
                                    index = (alpha: 3, red: 0, green: 1, blue: 2)
                                } else {
                                    index = nil
                                }
                                
                                if let index {
                                    var pixels = [[Double]]()
                                    var rgb: [Int: [Double]] = [:]
                                    
                                    for y in 0..<resizedImage.height {
                                        for x in 0..<resizedImage.width {
                                            let offset = y * resizedImage.bytesPerRow + x * channels
                                            let alpha = bytes[offset + index.alpha]
                                            
                                            if alpha > 0 {
                                                let r = Double(bytes[offset + index.red]) / Double(resizedImage.bitsPerPixel * 8 - 1)
                                                let g = Double(bytes[offset + index.green]) / Double(resizedImage.bitsPerPixel * 8 - 1)
                                                let b = Double(bytes[offset + index.blue]) / Double(resizedImage.bitsPerPixel * 8 - 1)
                                                let (hue, saturation, value) = self.rgbToHsv(red: r, green: g, blue: b)
                                                let (red, green, blue) = self.hsvToRgb(hue: hue, saturation: min(saturation * 1.0, 1.0), value: min(value * 1.0, 1.0))
                                                
                                                if saturation > 0.0 && 0.0 < value && value < 1.0 {
                                                    pixels.append([hue, saturation, value])
                                                }
                                                
                                                rgb[y * resizedImage.width + x] = [red, green, blue, Double(alpha) / Double(resizedImage.bitsPerPixel * 8 - 1)]
                                            }
                                        }
                                    }
                                    
                                    if !pixels.isEmpty {
                                        let kMeans = KMeans(numberOfClusters: 5)
                                        var stats = [Int: (count: Int, color: (hue: Double, saturation: Double, value: Double))]()
                                        var sum = 0
                                        var probabilities = [Double]()
                                        var palette = [CGColor]()
                                        let colorSpace = CGColorSpaceCreateDeviceRGB()
                                        let noiseScale = Double.random(in: 0.0005...0.005)
                                        let maxZ = 255
                                        
                                        kMeans.fit(data: pixels, iterations: 100)
                                        
                                        for pixel in pixels {
                                            let (id, vector) = kMeans.predict(vector: pixel)
                                            
                                            if let value = stats[id] {
                                                stats[id] = (count: value.count + 1, color: value.color)
                                            } else {
                                                stats[id] = (count: 1, color: (hue: vector[0], saturation: vector[1], value: vector[2]))
                                            }
                                            
                                            sum += 1
                                        }
                                        
                                        for (_, value) in stats {
                                            let (red, green, blue) = self.hsvToRgb(hue: value.color.hue, saturation: min(value.color.saturation * 2.0, 1.0), value: min(value.color.value * 2.0, 1.0))
                                            
                                            probabilities.append(Double(value.count) / Double(sum))
                                            palette.append(CGColor(colorSpace: colorSpace, components: [red, green, blue, 1.0])!)
                                        }
                                        
                                        UIGraphicsBeginImageContextWithOptions(CGSize(width: length, height: length), false, 0)
                                        
                                        if let context = UIGraphicsGetCurrentContext() {
                                            context.interpolationQuality = .default
                                            context.setAllowsAntialiasing(true)
                                            context.clear(CGRect(x: 0.0, y: 0.0, width: length, height: length))
                                            context.setLineWidth(1.0)
                                            context.setLineCap(.round)
                                            context.setLineJoin(.round)
                                            
                                            for _ in 0..<5000 {
                                                var x = floor(Double.random(in: 0..<length + Double(maxZ / 2)))
                                                var y = floor(Double.random(in: 0..<length))
                                                let mutablePath = CGMutablePath()
                                                
                                                mutablePath.move(to: CGPoint(x: x, y: y))
                                                
                                                for z in stride(from: 0, to: Int.random(in: maxZ / 2...maxZ), by: 2) {
                                                    var a = self.fBm(x: x * noiseScale, y: y * noiseScale, z: Double(z) * noiseScale * noiseScale, octaves: 1, persistence: 1.0) * .pi * 2.0
                                                    
                                                    x += cos(a)
                                                    y += sin(a)
                                                    
                                                    let point = CGPoint(x: x, y: y)
                                                    
                                                    a = self.fBm(x: x * noiseScale, y: y * noiseScale, z: Double(z + 1) * noiseScale * noiseScale, octaves: 1, persistence: 1.0) * .pi * 2.0
                                                    x += cos(a)
                                                    y += sin(a)
                                                    
                                                    mutablePath.addQuadCurve(to: CGPoint(x: x, y: y), control: point)
                                                }
                                                
                                                context.saveGState()
                                                context.setStrokeColor(palette[min(self.choice(probabilities: probabilities), probabilities.count - 1)])
                                                context.addPath(mutablePath)
                                                context.strokePath()
                                                context.restoreGState()
                                            }
                                            
                                            generatedImage = context.makeImage()
                                        }
                                        
                                        UIGraphicsEndImageContext()
                                    }
                                }
                            }
                        }
                    }
                    
                    return generatedImage
                }.value
            }
            
            self.isReloading = false
        }
    }
    
    private func emit(particles: Int) {
        for _ in 0..<particles {
            self.particles.append((layer: nil, time: 0.0, delay: Double.random(in: 0.0...1.0), duration: Double.random(in: 1.0...2.0)))
        }
    }
    
    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
        for touch in touches {
            let location = touch.location(in: self)
            
            self.touches.append((touch: touch, location: (x: location.x, y: location.y), movement: (x: 0.0, y: 0.0), velocity: (x: 0.0, y: 0.0), timestamp: touch.timestamp))
        }
        
        if self.touches.count == 1 {
            self.tracker.active = true
            self.tracker.velocity.x = 0.0
            self.tracker.velocity.y = 0.0
            self.touches[0].movement.x = self.tracker.movement.x
            self.touches[0].movement.y = self.tracker.movement.y
        } else if !self.pinches.contains(where: { $0.touches.allSatisfy({ (touch: UITouch) -> Bool in self.touches.contains(where: { $0.touch == touch }) }) }) {
            var centerX = 0.0
            var centerY = 0.0
            var sum = 0.0
            var tempTouches: [UITouch] = []
            
            self.tracker.active = false
            self.tracker.velocity.x = 0.0
            self.tracker.velocity.y = 0.0
            
            for tuple in self.touches {
                tempTouches.append(tuple.touch)
                centerX += tuple.location.x
                centerY += tuple.location.y
            }
            
            centerX /= Double(self.touches.count)
            centerY /= Double(self.touches.count)
            
            for touch in self.touches {
                sum += sqrt((centerX - touch.location.x) * (centerX - touch.location.x) + (centerY - touch.location.y) * (centerY - touch.location.y))
            }
            
            self.pinches.append((touches: tempTouches, center: (x: centerX, y: centerY), movement: (x: 0.0, y: 0.0), radius: sum / Double(self.touches.count), velocity: 0.0, current: (x: centerX, y: centerY, radius: 0.0)))
        }
    }
    
    override func touchesMoved(_ touches: Set<UITouch>, with event: UIEvent?) {
        for touch in touches {
            if let index = self.touches.firstIndex(where: { $0.touch == touch }) {
                let location = touch.location(in: self)
                let previousLocation = touch.previousLocation(in: self)
                let deltaX = location.x - previousLocation.x
                let deltaY = location.y - previousLocation.y
                let deltaTime = touch.timestamp - self.touches[index].timestamp
                
                self.touches[index].location.x = location.x
                self.touches[index].location.y = location.y
                self.touches[index].movement.x += deltaX
                self.touches[index].movement.y += deltaY
                self.touches[index].timestamp = touch.timestamp
                
                if deltaTime > 0 {
                    self.touches[index].velocity.x = max(min(deltaX / deltaTime, 1000.0), -1000.0)
                    self.touches[index].velocity.y = max(min(deltaY / deltaTime, 1000.0), -1000.0)
                }
            }
        }
        
        if self.touches.count == 1 {
            let lineHeight = self.frame.size.height / Double(self.blocks.count)
            let fontSize = ceil(self.blocks.count == 1 ? lineHeight : lineHeight / 1.5)
            
            if let frames = self.backgroundFrames {
                var frame = frames[0]
                
                if let currentTime = self.currentTime, currentTime >= frame.delay {
                    var delay = frame.delay
                    
                    for i in 1..<frames.count {
                        frame = frames[i]
                        delay += frame.delay
                        
                        if currentTime < delay {
                            break
                        }
                    }
                }
                
                if let image = frame.image {
                    let screenAspect = self.frame.size.width / self.frame.size.height
                    let imageAspect = Double(image.width) / Double(image.height)
                    
                    if screenAspect > imageAspect {
                        self.tracker.movement.y = self.touches[0].movement.y
                        self.tracker.velocity.y = self.touches[0].velocity.y
                    } else {
                        self.tracker.movement.x = self.touches[0].movement.x
                        self.tracker.velocity.x = self.touches[0].velocity.x
                    }
                }
            }
            
            for index in 0..<self.blocks.count {
                let y = lineHeight * Double(index) + (lineHeight - fontSize) / 2.0
                
                if y <= self.touches[0].location.y && self.touches[0].location.y < y + fontSize && self.blocks[index].scroll.touch == nil {
                    self.blocks[index].scroll.touch = self.touches[0].touch
                }
            }
        } else if let index = self.pinches.firstIndex(where: { $0.touches.allSatisfy({ (touch: UITouch) -> Bool in self.touches.contains(where: { $0.touch == touch }) }) }) {
            var movementX = 0.0
            var movementY = 0.0
            var sum = 0.0
            
            for touch in self.touches {
                movementX += touch.movement.x
                movementY += touch.movement.y
            }
            
            movementX /= Double(self.touches.count)
            movementY /= Double(self.touches.count)
            
            for touch in self.touches {
                sum += sqrt((self.pinches[index].center.x + movementX - touch.location.x) * (self.pinches[index].center.x + movementX - touch.location.x) + (self.pinches[index].center.y + movementY - touch.location.y) * (self.pinches[index].center.y + movementY - touch.location.y))
            }
            
            self.pinches[index].movement.x = movementX
            self.pinches[index].movement.y = movementY
            self.pinches[index].radius = sum / Double(self.touches.count)
        }
    }
    
    override func touchesEnded(_ touches: Set<UITouch>, with event: UIEvent?) {
        self.tracker.active = false
        
        if self.touches.count == 1 {
            let lineHeight = self.frame.size.height / Double(self.blocks.count)
            let fontSize = ceil(self.blocks.count == 1 ? lineHeight : lineHeight / 1.5)
            
            for index in 0..<self.blocks.count {
                let y = lineHeight * Double(index) + (lineHeight - fontSize) / 2.0
                
                if y <= self.touches[0].location.y && self.touches[0].location.y < y + fontSize {
                    let point = CGPoint(x: self.touches[0].location.x, y: self.touches[0].location.y)
                    
                    if !self.subviews.contains(where: { $0 is UIVisualEffectView == false && $0.hitTest(point, with: nil) != nil }), let delegate = self.delegate {
                        if delegate.wallCanSelect(self, at: index) {
                            if self.blocks[index].scroll.touch == nil {
                                self.blocks[index].scroll.touch = self.touches[0].touch
                            }
                            
                            delegate.wallDidSelect(self, at: index)
                        } else {
                            self.blocks[index].shake.time = 0.0
                        }
                    }
                }
            }
        }
        
        for touch in touches {
            if let index = self.touches.firstIndex(where: { $0.touch == touch }) {
                self.touches.remove(at: index)
            }
        }
    }
    
    override func touchesCancelled(_ touches: Set<UITouch>, with event: UIEvent?) {
        self.tracker.active = false
        
        for touch in touches {
            if let index = self.touches.firstIndex(where: { $0.touch == touch }) {
                self.touches.remove(at: index)
            }
        }
    }
    
    @objc private func update(displayLink: CADisplayLink) {
        if self.frame.size.width > 0 && self.frame.size.height > 0 {
            let deltaTime = displayLink.targetTimestamp - displayLink.timestamp
            
            if self.isInvalidated {
                for i in 0..<self.blocks.count {
                    var block = self.blocks[i]
                    
                    if block.running {
                        block.type.reverse = true
                    }
                    
                    self.blocks[i] = block
                }
                
                self.isInvalidated = false
            }
            
            if self.blocks.first(where: { $0.running || $0.type.elapsed >= 0.0 || $0.type.reverse }) == nil {
                self.blocks.removeAll()
                self.fontCache.removeAll()
                self.textCache.removeAll()
                
                for (i, line) in self.lines.enumerated() {
                    self.blocks.append((running: true, time: 0.0, duration: -1.0, type: (elapsed: -1.0, speed: 50.0, reverse: false, buffer: String(), count: 0), text: line.text, attributes: line.attributes, current: String(), safe: [], scroll: (touch: nil, step: 0.0), shake: (time: nil, x: 0.0), elapsed: Double.random(in: 0.0..<60.0), rtl: i % 2 == 1))
                }
            }
            
            for i in 0..<self.blocks.count {
                var block = self.blocks[i]
                
                if block.running {
                    if block.rtl {
                        if block.type.reverse {
                            if block.type.count > 0 {
                                block.type.elapsed += deltaTime * 2
                                
                                if block.type.elapsed >= 1.0 / block.type.speed {
                                    if block.type.count - 1 < block.text.count {
                                        let width = block.text.count / 2
                                        
                                        if block.type.buffer.count <= width && block.type.count > 0 {
                                            block.type.count -= 1
                                        }
                                        
                                        if !block.type.buffer.isEmpty {
                                            block.type.buffer.remove(at: block.type.buffer.startIndex)
                                        }
                                    }
                                    
                                    block.type.elapsed = 0.0
                                }
                            } else {
                                block.time = 0.0
                                block.type.elapsed = -1.0
                                block.type.reverse = false
                                block.running = false
                            }
                        } else if block.type.buffer.count < block.text.count {
                            if block.type.elapsed >= 0.0 {
                                block.type.elapsed += deltaTime
                            } else {
                                block.type.elapsed = deltaTime
                            }
                            
                            if block.type.elapsed >= 1.0 / block.type.speed {
                                if block.type.count >= block.text.count / 2 {
                                    block.type.buffer.insert(block.text[block.text.index(block.text.endIndex, offsetBy: -block.type.buffer.count - 1)], at: block.type.buffer.startIndex)
                                }
                                
                                if block.type.count < block.text.count {
                                    block.type.count += 1
                                }
                                
                                block.type.elapsed = 0.0
                            }
                        } else {
                            block.time += deltaTime
                            
                            if block.duration >= 0.0 && block.time >= block.duration {
                                block.type.reverse = true
                            }
                        }
                        
                        if block.text.count == block.type.buffer.count {
                            block.current.removeAll()
                            block.current.append(block.text)
                        } else {
                            var characters = [Character]()
                            var randomBuffer = String()
                            
                            for k in 0..<block.text.count {
                                let character = block.text[block.text.index(block.text.startIndex, offsetBy: k)]
                                
                                if !character.isNewline && !character.isWhitespace {
                                    characters.append(character)
                                }
                            }
                            
                            if !characters.isEmpty {
                                for k in 0..<block.type.count {
                                    let character = block.text[block.text.index(block.text.startIndex, offsetBy: k)]
                                    
                                    if character.isNewline {
                                        randomBuffer.append(character)
                                    } else {
                                        randomBuffer.append(characters[Int.random(in: 0..<characters.count)])
                                    }
                                }
                            }
                            
                            if randomBuffer.count > block.type.buffer.count {
                                block.current.removeAll()
                                block.current.append(String(randomBuffer[randomBuffer.index(randomBuffer.startIndex, offsetBy: 0)..<randomBuffer.index(randomBuffer.startIndex, offsetBy: randomBuffer.count - block.type.buffer.count)]))
                                block.current.append(block.type.buffer)
                            } else if block.current.count != block.type.buffer.count {
                                block.current.removeAll()
                                block.current.append(block.type.buffer)
                            }
                        }
                    } else {
                        if block.type.reverse {
                            if block.type.count > 0 {
                                block.type.elapsed += deltaTime * 2
                                
                                if block.type.elapsed >= 1.0 / block.type.speed {
                                    if block.type.count - 1 < block.text.count {
                                        let width = block.text.count / 2
                                        
                                        if block.type.buffer.count <= width && block.type.count > 0 {
                                            block.type.count -= 1
                                        }
                                        
                                        if !block.type.buffer.isEmpty {
                                            block.type.buffer.remove(at: block.type.buffer.index(block.type.buffer.endIndex, offsetBy: -1))
                                        }
                                    }
                                    
                                    block.type.elapsed = 0.0
                                }
                            } else {
                                block.time = 0.0
                                block.type.elapsed = -1.0
                                block.type.reverse = false
                                block.running = false
                            }
                        } else if block.type.buffer.count < block.text.count {
                            if block.type.elapsed >= 0.0 {
                                block.type.elapsed += deltaTime
                            } else {
                                block.type.elapsed = deltaTime
                            }
                            
                            if block.type.elapsed >= 1.0 / block.type.speed {
                                if block.type.count >= block.text.count / 2 {
                                    block.type.buffer.append(block.text[block.text.index(block.text.startIndex, offsetBy: block.type.buffer.count)])
                                }
                                
                                if block.type.count < block.text.count {
                                    block.type.count += 1
                                }
                                
                                block.type.elapsed = 0.0
                            }
                        } else {
                            block.time += deltaTime
                            
                            if block.duration >= 0.0 && block.time >= block.duration {
                                block.type.reverse = true
                            }
                        }
                        
                        if block.text.count == block.type.buffer.count {
                            block.current.removeAll()
                            block.current.append(block.text)
                        } else {
                            var characters = [Character]()
                            var randomBuffer = String()
                            
                            for k in 0..<block.text.count {
                                let character = block.text[block.text.index(block.text.startIndex, offsetBy: k)]
                                
                                if !character.isNewline && !character.isWhitespace {
                                    characters.append(character)
                                }
                            }
                            
                            if !characters.isEmpty {
                                for k in 0..<block.type.count {
                                    let character = block.text[block.text.index(block.text.startIndex, offsetBy: k)]
                                    
                                    if character.isNewline {
                                        randomBuffer.append(character)
                                    } else {
                                        randomBuffer.append(characters[Int.random(in: 0..<characters.count)])
                                    }
                                }
                            }
                            
                            if randomBuffer.count > block.type.buffer.count {
                                block.current.removeAll()
                                block.current.append(block.type.buffer)
                                block.current.append(String(randomBuffer[randomBuffer.index(randomBuffer.startIndex, offsetBy: block.type.buffer.count)..<randomBuffer.index(randomBuffer.startIndex, offsetBy: randomBuffer.count)]))
                            } else if block.current.count != block.type.buffer.count {
                                block.current.removeAll()
                                block.current.append(block.type.buffer)
                            }
                        }
                    }
                    
                    if block.scroll.touch != nil {
                        block.scroll.step += deltaTime
                        
                        if block.scroll.step >= 1.0 {
                            block.scroll.touch = nil
                            block.scroll.step = 0.0
                        }
                    }
                    
                    if let time = block.shake.time {
                        let t = time + deltaTime
                        let duration = 0.5
                        
                        if t >= duration {
                            block.shake.time = nil
                            block.shake.x = 0.0
                        } else {
                            let length = 5.0
                            let interval = duration / length / 3.0
                            
                            block.shake.time = t
                            
                            if fmod(t, interval) > 0.0 {
                                let x = Int(length * 3.0 - floor(t / interval))
                                let remainder = x % 3
                                
                                if remainder == 0 {
                                    block.shake.x = Double(x / 3)
                                } else if remainder == 1 {
                                    block.shake.x = -Double(x / 3 + 1)
                                } else {
                                    block.shake.x = 0.0
                                }
                            }
                        }
                    } else {
                        block.elapsed += deltaTime
                    }
                }
                
                self.blocks[i] = block
            }
            
            if self.isLoading {
                if self.isFetched {
                    let step = self.revealStep + deltaTime
                    
                    if step >= 1.0 {
                        if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer {
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            blindLayer.frame = CGRect.zero
                            loadingLayer.frame = CGRect.zero
                            
                            CATransaction.commit()
                        }
                        
                        self.revealStep = -1.0
                        self.loadingStep = 0.0
                        self.isLoading = false
                    } else {
                        if let blindLayer = self.blindLayer, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                            let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height)
                            
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            blindLayer.frame = CGRect(x: 0.0, y: -length * sin(step / 2.0 * Double.pi), width: length, height: length)
                            
                            CATransaction.commit()
                        }
                        
                        self.loadingStep += deltaTime
                        
                        if self.loadingStep >= 1.0 {
                            self.loadingStep = 0.0
                        }
                        
                        if let loadingLayer = self.loadingLayer {
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                            
                            CATransaction.commit()
                        }
                        
                        self.revealStep = step
                    }
                } else if self.revealStep > 0.0 {
                    let step = self.revealStep - deltaTime
                    
                    self.loadingStep += deltaTime
                    
                    if self.loadingStep >= 1.0 {
                        self.loadingStep = 0.0
                    }
                    
                    if step <= 0.0 {
                        if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                            let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height)
                            
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            blindLayer.frame = CGRect(x: 0.0, y: 0.0, width: length, height: length)
                            loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                            
                            CATransaction.commit()
                        }
                        
                        self.revealStep = 0.0
                        
                        if !self.isReloading {
                            if let image = self.fetchedImage {
                                if let backgroundLayer = self.backgroundLayer, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                                    CATransaction.begin()
                                    CATransaction.setDisableActions(true)
                                    
                                    backgroundLayer.frame = CGRect(x: 0.0, y: 0.0, width: Double(image.width) / window.screen.scale, height: Double(image.height) / window.screen.scale)
                                    backgroundLayer.contents = image
                                    
                                    CATransaction.commit()
                                }
                            } else if let backgroundLayer = self.backgroundLayer {
                                CATransaction.begin()
                                CATransaction.setDisableActions(true)
                                
                                backgroundLayer.frame = CGRect.zero
                                backgroundLayer.contents = nil
                                
                                CATransaction.commit()
                            }
                            
                            self.backgroundFrames = self.fetchedFrames
                            self.currentTime = nil
                            self.sourceRect = CGRect.zero
                            self.isFetched = true
                            self.tracker.movement.x = 0.0
                            self.tracker.movement.y = 0.0
                            self.tracker.velocity.x = 0.0
                            self.tracker.velocity.y = 0.0
                            self.pinches.removeAll()
                            self.touches.removeAll()
                            self.emit(particles: self.requestParticles)
                            self.requestParticles = 0
                        }
                    } else {
                        if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                            let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height)
                            
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            blindLayer.frame = CGRect(x: 0.0, y: -length * sin(step / 2.0 * Double.pi), width: length, height: length)
                            loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                            
                            CATransaction.commit()
                        }
                        
                        self.revealStep = step
                    }
                } else {
                    let step = self.revealStep + deltaTime
                    
                    if step >= 0.0 {
                        if self.loadingStep < 0.0 {
                            self.loadingStep = 0.0
                            
                            if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                                let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height)
                                
                                CATransaction.begin()
                                CATransaction.setDisableActions(true)
                                
                                blindLayer.frame = CGRect(x: 0.0, y: 0.0, width: length, height: length)
                                loadingLayer.frame = CGRect(x: 0.0, y: 0.0, width: length + self.backgroundPattern.size.width, height: length)
                                
                                CATransaction.commit()
                            }
                        } else {
                            self.loadingStep += deltaTime
                            
                            if self.loadingStep >= 1.0 {
                                self.loadingStep = 0.0
                            }
                            
                            if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                                let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height)
                                
                                CATransaction.begin()
                                CATransaction.setDisableActions(true)
                                
                                blindLayer.frame = CGRect(x: 0.0, y: 0.0, width: length, height: length)
                                loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                                
                                CATransaction.commit()
                            }
                        }
                        
                        self.revealStep = 0.0
                        
                        if !self.isReloading {
                            if let image = self.fetchedImage {
                                if let backgroundLayer = self.backgroundLayer, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                                    CATransaction.begin()
                                    CATransaction.setDisableActions(true)
                                    
                                    backgroundLayer.frame = CGRect(x: 0.0, y: 0.0, width: Double(image.width) / window.screen.scale, height: Double(image.height) / window.screen.scale)
                                    backgroundLayer.contents = image
                                    
                                    CATransaction.commit()
                                }
                            } else if let backgroundLayer = self.backgroundLayer {
                                CATransaction.begin()
                                CATransaction.setDisableActions(true)
                                
                                backgroundLayer.frame = CGRect.zero
                                backgroundLayer.contents = nil
                                
                                CATransaction.commit()
                            }
                            
                            self.backgroundFrames = self.fetchedFrames
                            self.currentTime = nil
                            self.sourceRect = CGRect.zero
                            self.isFetched = true
                            self.tracker.movement.x = 0.0
                            self.tracker.movement.y = 0.0
                            self.tracker.velocity.x = 0.0
                            self.tracker.velocity.y = 0.0
                            self.pinches.removeAll()
                            self.touches.removeAll()
                            self.emit(particles: self.requestParticles)
                            self.requestParticles = 0
                        }
                    } else {
                        if self.revealStep > -1.0 {
                            if let blindLayer = self.blindLayer, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                                let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height)
                                
                                CATransaction.begin()
                                CATransaction.setDisableActions(true)
                                
                                blindLayer.frame = CGRect(x: 0.0, y: -length * sin(step / 2.0 * Double.pi), width: length, height: length)
                                
                                CATransaction.commit()
                            }
                            
                            self.loadingStep += deltaTime
                            
                            if self.loadingStep >= 1.0 {
                                self.loadingStep = 0.0
                            }
                            
                            if let loadingLayer = self.loadingLayer {
                                CATransaction.begin()
                                CATransaction.setDisableActions(true)
                                
                                loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                                
                                CATransaction.commit()
                            }
                        } else if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                            let length = max(window.screen.bounds.size.width, window.screen.bounds.size.height)
                            
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            blindLayer.frame = CGRect(x: 0.0, y: -length * sin(step / 2.0 * Double.pi), width: length, height: length)
                            blindLayer.backgroundColor = self.blindColor
                            loadingLayer.frame = CGRect(x: 0.0, y: 0.0, width: length + self.backgroundPattern.size.width, height: length)
                            
                            CATransaction.commit()
                        }
                        
                        self.revealStep = step
                    }
                }
            }
            
            if let subview = self.subviews.first(where: { $0 is UIVisualEffectView }), let maskLayer = self.maskLayer {
                var renderRequired = false
                
                self.time += deltaTime
                
                if self.time >= deltaTime * 2.0 {
                    self.time = 0.0
                    renderRequired = true
                } else if maskLayer.frame.size.width != self.frame.size.width || maskLayer.frame.size.height != self.frame.size.height {
                    renderRequired = true
                }
                
                if renderRequired {
                    typealias Segment = (text: String, framesetter: CTFramesetter?, size: CGSize)
                    let blackColor = CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [0.0, 0.0, 0.0, 1.0])
                    let lineHeight = self.frame.size.height / Double(self.blocks.count)
                    let fontSize = ceil(self.blocks.count == 1 ? lineHeight : lineHeight / 1.5)
                    let margin = ceil(fontSize / 2.0)
                    let fontName = "Futura-Bold"
                    let font: CTFont
                    let mutablePath = CGMutablePath()
                    var paths: [CGPath] = []
                    
                    if let cachedFont = self.fontCache[fontName] {
                        if CTFontGetSize(cachedFont) == fontSize {
                            font = cachedFont
                        } else {
                            font = CTFontCreateWithName(fontName as CFString, fontSize, nil)
                            self.fontCache[fontName] = font
                        }
                    } else {
                        font = CTFontCreateWithName(fontName as CFString, fontSize, nil)
                        self.fontCache[fontName] = font
                    }
                    
                    for (i, block) in self.blocks.enumerated() {
                        if block.running && !block.current.isEmpty {
                            var current: [Segment] = []
                            var target: [(text: String, size: CGSize)] = []
                            var index = 0
                            var currentWidth = 0.0
                            var targetWidth = 0.0
                            var offset = 0.0
                            
                            while index < block.current.count {
                                if let attribute = block.attributes.first(where: { index >= $0.start && index < $0.end }) {
                                    if attribute.end <= block.current.count {
                                        current.append((text: String(block.current[block.current.index(block.current.startIndex, offsetBy: index)..<block.current.index(block.current.startIndex, offsetBy: attribute.end)]), framesetter: nil, size: CGSize.zero))
                                        index = attribute.end
                                    } else {
                                        current.append((text: String(block.current[block.current.index(block.current.startIndex, offsetBy: index)..<block.current.index(block.current.startIndex, offsetBy: block.current.count)]), framesetter: nil, size: CGSize.zero))
                                        
                                        break
                                    }
                                } else {
                                    var minimum: (start: Int?, distance: Int) = (start: nil, distance: Int.max)
                                    
                                    for attribute in block.attributes {
                                        let distance = attribute.start - index
                                        
                                        if distance >= 0 && distance < minimum.distance {
                                            minimum.distance = distance
                                            minimum.start = attribute.start
                                        }
                                    }
                                    
                                    if let start = minimum.start {
                                        if start <= block.current.count {
                                            current.append((text: String(block.current[block.current.index(block.current.startIndex, offsetBy: index)..<block.current.index(block.current.startIndex, offsetBy: start)]), framesetter: nil, size: CGSize.zero))
                                            index = start
                                        } else {
                                            current.append((text: String(block.current[block.current.index(block.current.startIndex, offsetBy: index)..<block.current.index(block.current.startIndex, offsetBy: block.current.count)]), framesetter: nil, size: CGSize.zero))
                                            
                                            break
                                        }
                                    } else {
                                        current.append((text: String(block.current[block.current.index(block.current.startIndex, offsetBy: index)..<block.current.index(block.current.startIndex, offsetBy: block.current.count)]), framesetter: nil, size: CGSize.zero))
                                        
                                        break
                                    }
                                }
                            }
                            
                            for j in 0..<current.count {
                                var segment = current[j]
                                let key = "\(fontName)&\(fontSize)&\(segment.text)"
                                var frameSize: CGSize
                                
                                if let value = self.textCache[key] {
                                    segment.framesetter = value.0
                                    frameSize = value.1
                                } else {
                                    let framesetter = self.createFramesetter(font: font, color: blackColor!, text: segment.text)
                                    var spaces = 0
                                    
                                    segment.framesetter = framesetter
                                    frameSize = CTFramesetterSuggestFrameSizeWithConstraints(framesetter, CFRange(), nil, CGSize(width: CGFloat.greatestFiniteMagnitude, height: CGFloat.greatestFiniteMagnitude), nil)
                                    
                                    for char in segment.text.reversed() {
                                        if char.isWhitespace {
                                            spaces += 1
                                        } else {
                                            break
                                        }
                                    }
                                    
                                    if spaces > 0 {
                                        var spaceCharacter: UniChar = 0x0020
                                        var glyph: CGGlyph = 0
                                        
                                        if CTFontGetGlyphsForCharacters(font, &spaceCharacter, &glyph, 1) {
                                            var advance = CGSize()
                                            
                                            CTFontGetAdvancesForGlyphs(font, .horizontal, &glyph, &advance, 1)
                                            
                                            frameSize.width += advance.width * Double(spaces)
                                        }
                                    }
                                }
                                
                                currentWidth += ceil(frameSize.width)
                                segment.size = frameSize
                                current[j] = segment
                            }
                            
                            index = 0
                            
                            while index < block.text.count {
                                if let attribute = block.attributes.first(where: { index >= $0.start && index < $0.end }) {
                                    target.append((text: String(block.text[block.text.index(block.text.startIndex, offsetBy: index)..<block.text.index(block.text.startIndex, offsetBy: attribute.end)]), size: CGSize.zero))
                                    index = attribute.end
                                } else {
                                    var minimum: (start: Int?, distance: Int) = (start: nil, distance: Int.max)
                                    
                                    for attribute in block.attributes {
                                        let distance = attribute.start - index
                                        
                                        if distance >= 0 && distance < minimum.distance {
                                            minimum.distance = distance
                                            minimum.start = attribute.start
                                        }
                                    }
                                    
                                    if let start = minimum.start {
                                        target.append((text: String(block.text[block.text.index(block.text.startIndex, offsetBy: index)..<block.text.index(block.text.startIndex, offsetBy: start)]), size: CGSize.zero))
                                        index = start
                                    } else {
                                        target.append((text: String(block.text[block.text.index(block.text.startIndex, offsetBy: index)..<block.text.index(block.text.startIndex, offsetBy: block.text.count)]), size: CGSize.zero))
                                        
                                        break
                                    }
                                }
                            }
                            
                            for j in 0..<target.count {
                                var segment = target[j]
                                let key = "\(fontName)&\(fontSize)&\(segment.text)"
                                var frameSize: CGSize
                                
                                if let value = self.textCache[key] {
                                    frameSize = value.1
                                } else {
                                    let framesetter = self.createFramesetter(font: font, color: blackColor!, text: segment.text)
                                    var spaces = 0
                                    
                                    frameSize = CTFramesetterSuggestFrameSizeWithConstraints(framesetter, CFRange(), nil, CGSize(width: CGFloat.greatestFiniteMagnitude, height: CGFloat.greatestFiniteMagnitude), nil)
                                    
                                    for char in segment.text.reversed() {
                                        if char.isWhitespace {
                                            spaces += 1
                                        } else {
                                            break
                                        }
                                    }
                                    
                                    if spaces > 0 {
                                        var spaceCharacter: UniChar = 0x0020
                                        var glyph: CGGlyph = 0
                                        
                                        if CTFontGetGlyphsForCharacters(font, &spaceCharacter, &glyph, 1) {
                                            var advance = CGSize()
                                            
                                            CTFontGetAdvancesForGlyphs(font, .horizontal, &glyph, &advance, 1)
                                            
                                            frameSize.width += advance.width * Double(spaces)
                                        }
                                    }
                                    
                                    self.textCache[key] = (framesetter, frameSize, nil)
                                }
                                
                                targetWidth += ceil(frameSize.width)
                                segment.size = frameSize
                                target[j] = segment
                            }
                            
                            if currentWidth > targetWidth {
                                current.removeAll()
                                current.append(contentsOf: block.safe)
                            } else {
                                self.blocks[i].safe = current
                            }
                            
                            let translation = fmod(fmod(block.elapsed, 60.0) / 60.0 + sin(block.scroll.step / 2.0 * .pi), 1.0) * -(targetWidth + margin) + block.shake.x
                            
                            if block.rtl {
                                repeat {
                                    for _ in 0..<2 {
                                        var x = 0.0
                                        
                                        for segment in current.reversed() {
                                            if offset - translation - x >= 0 && offset - translation - x - segment.size.width < self.frame.size.width {
                                                let key = "\(fontName)&\(fontSize)&\(segment.text)"
                                                let transform = CGAffineTransformConcat(CGAffineTransformMakeTranslation(offset - translation - round(x) - ceil(segment.size.width), self.frame.size.height - round(lineHeight * Double(i) + (lineHeight - segment.size.height) / 2.0)), CGAffineTransformConcat(CGAffineTransformMakeScale(1.0, -1.0), CGAffineTransformMakeTranslation(0.0, self.frame.size.height)))
                                                
                                                if let value = self.textCache[key] {
                                                    if let path = value.2 {
                                                        mutablePath.addPath(path, transform: transform)
                                                    } else {
                                                        let path = self.createTextPath(frame: CTFramesetterCreateFrame(segment.framesetter!, CFRange(), CGPath(rect: CGRect(origin: CGPoint.zero, size: segment.size), transform: nil), nil))
                                                        
                                                        mutablePath.addPath(path, transform: transform)
                                                        self.textCache[key] = (value.0, value.1, path)
                                                    }
                                                } else {
                                                    mutablePath.addPath(self.createTextPath(frame: CTFramesetterCreateFrame(segment.framesetter!, CFRange(), CGPath(rect: CGRect(origin: CGPoint.zero, size: segment.size), transform: nil), nil)), transform: transform)
                                                }
                                            }
                                            
                                            x += ceil(segment.size.width)
                                        }
                                        
                                        for segment in target {
                                            offset += ceil(segment.size.width)
                                        }
                                        
                                        offset += margin
                                    }
                                } while offset - margin < self.frame.size.width * 2.0
                            } else {
                                repeat {
                                    for _ in 0..<2 {
                                        var x = 0.0
                                        
                                        for segment in current {
                                            if translation + offset + x + segment.size.width >= 0 && translation + offset + x < self.frame.size.width {
                                                let key = "\(fontName)&\(fontSize)&\(segment.text)"
                                                let transform = CGAffineTransformConcat(CGAffineTransformMakeTranslation(translation + offset + round(x), self.frame.size.height - round(lineHeight * Double(i) + (lineHeight - segment.size.height) / 2.0)), CGAffineTransformConcat(CGAffineTransformMakeScale(1.0, -1.0), CGAffineTransformMakeTranslation(0.0, self.frame.size.height)))
                                                
                                                if let value = self.textCache[key] {
                                                    if let path = value.2 {
                                                        mutablePath.addPath(path, transform: transform)
                                                    } else {
                                                        let path = self.createTextPath(frame: CTFramesetterCreateFrame(segment.framesetter!, CFRange(), CGPath(rect: CGRect(origin: CGPoint.zero, size: segment.size), transform: nil), nil))
                                                        
                                                        mutablePath.addPath(path, transform: transform)
                                                        self.textCache[key] = (value.0, value.1, path)
                                                    }
                                                } else {
                                                    mutablePath.addPath(self.createTextPath(frame: CTFramesetterCreateFrame(segment.framesetter!, CFRange(), CGPath(rect: CGRect(origin: CGPoint.zero, size: segment.size), transform: nil), nil)), transform: transform)
                                                }
                                            }
                                            
                                            x += ceil(segment.size.width)
                                        }
                                        
                                        for segment in target {
                                            offset += ceil(segment.size.width)
                                        }
                                        
                                        offset += margin
                                    }
                                } while offset - margin < self.frame.size.width * 2.0
                            }
                        }
                    }
                    
                    for i in stride(from: self.pinches.count - 1, through: 0, by: -1) {
                        if self.pinches[i].touches.allSatisfy({ (touch: UITouch) -> Bool in self.touches.contains(where: { $0.touch == touch }) }) {
                            let speed = 2.0
                            let x = self.pinches[i].center.x + self.pinches[i].movement.x
                            let y = self.pinches[i].center.y + self.pinches[i].movement.y
                            let radius = abs(self.pinches[i].radius)
                            
                            self.pinches[i].current.x = self.lerp(a: self.pinches[i].current.x, b: x, t: deltaTime * speed)
                            self.pinches[i].current.y = self.lerp(a: self.pinches[i].current.y, b: y, t: deltaTime * speed)
                            self.pinches[i].current.radius = self.lerp(a: abs(self.pinches[i].current.radius), b: radius, t: deltaTime * speed)
                            
                            if round(self.pinches[i].current.x) == round(x) {
                                self.pinches[i].current.x = x
                            }
                            
                            if round(self.pinches[i].current.y) == round(y) {
                                self.pinches[i].current.y = y
                            }
                            
                            if round(self.pinches[i].current.radius) == round(radius) {
                                self.pinches[i].current.radius = radius
                            }
                            
                            paths.append(CGPath(ellipseIn: CGRect(x: self.pinches[i].current.x - self.pinches[i].current.radius, y: self.pinches[i].current.y - self.pinches[i].current.radius, width: self.pinches[i].current.radius * 2.0, height: self.pinches[i].current.radius * 2.0), transform: nil))
                        } else {
                            let epsilon = 0.01
                            let tension = 50.0
                            let mass = 1.0
                            let friction = 5.0
                            let displacement = self.pinches[i].current.radius
                            let tensionForce = -tension * displacement
                            let dampingForce = -friction * self.pinches[i].velocity
                            let acceleration = (tensionForce + dampingForce) / mass
                            
                            self.pinches[i].velocity += acceleration * deltaTime
                            self.pinches[i].current.radius += self.pinches[i].velocity * deltaTime
                            
                            if abs(self.pinches[i].velocity) < epsilon {
                                self.pinches.remove(at: i)
                            } else {
                                let radius = abs(self.pinches[i].current.radius)
                                
                                paths.append(CGPath(ellipseIn: CGRect(x: self.pinches[i].current.x - radius, y: self.pinches[i].current.y - radius, width: radius * 2.0, height: radius * 2.0), transform: nil))
                            }
                        }
                    }
                    
                    if mutablePath.isEmpty {
                        subview.layer.mask = nil
                    } else {
                        CATransaction.begin()
                        CATransaction.setDisableActions(true)
                        
                        maskLayer.frame = CGRect(origin: CGPoint.zero, size: self.frame.size)
                        maskLayer.path = mutablePath
                        
                        if let sublayers = maskLayer.sublayers {
                            for sublayer in sublayers {
                                if let shapeLayer = sublayer as? CAShapeLayer {
                                    shapeLayer.frame = CGRect(origin: CGPoint.zero, size: self.frame.size)
                                    
                                    if !paths.isEmpty {
                                        var path: CGPath = paths[0]
                                        
                                        for i in 1..<paths.count {
                                            path = path.union(paths[i], using: .winding)
                                        }
                                        
                                        shapeLayer.path = path
                                    }
                                }
                            }
                        }
                        
                        CATransaction.commit()
                    }
                    
                    subview.layer.mask = maskLayer
                }
            }
            
            if !self.tracker.active && (self.tracker.velocity.x != 0.0 || self.tracker.velocity.y != 0.0) {
                let epsilon = 0.01
                let decelerationRate = 10 * 72 / 1000.0

                if self.tracker.velocity.x > 1000.0 {
                    self.tracker.velocity.x = 1000.0
                } else if self.tracker.velocity.x < -1000.0 {
                    self.tracker.velocity.x = -1000
                }

                if self.tracker.velocity.y > 1000.0 {
                    self.tracker.velocity.y = 1000.0
                } else if self.tracker.velocity.y < -1000.0 {
                    self.tracker.velocity.y = -1000.0
                }

                self.tracker.velocity.x -= self.tracker.velocity.x * decelerationRate * deltaTime
                self.tracker.velocity.y -= self.tracker.velocity.y * decelerationRate * deltaTime

                if abs(self.tracker.velocity.x) < epsilon {
                    self.tracker.velocity.x = 0.0
                }

                if abs(self.tracker.velocity.y) < epsilon {
                    self.tracker.velocity.y = 0.0
                }

                self.tracker.movement.x += self.tracker.velocity.x * deltaTime
                self.tracker.movement.y += self.tracker.velocity.y * deltaTime
            }
            
            if let frames = self.backgroundFrames {
                var image: CGImage? = nil
                var updateRequired = false
                
                if let currentTime = self.currentTime {
                    if frames.count > 1 {
                        let nextTime = currentTime + deltaTime
                        var previousImage: CGImage? = nil
                        var nextImage: CGImage? = nil
                        var delay = 0.0
                        
                        for frame in frames {
                            let nextDelay = delay + frame.delay
                            
                            if currentTime >= delay && currentTime < nextDelay {
                                previousImage = frame.image
                            }
                            
                            if nextTime >= delay && nextTime < nextDelay {
                                nextImage = frame.image
                            }
                            
                            delay = nextDelay
                        }
                        
                        if nextImage == nil {
                            if previousImage != frames[0].image {
                                updateRequired = true
                            }
                            
                            image = frames[0].image
                            self.currentTime = 0.0
                        } else if previousImage == nextImage {
                            image = nextImage
                            self.currentTime = nextTime
                        } else {
                            updateRequired = true
                            image = nextImage
                            self.currentTime = nextTime
                        }
                    } else {
                        image = frames[0].image
                    }
                } else {
                    image = frames[0].image
                    updateRequired = true
                    self.currentTime = 0.0
                }
                
                if let image, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
                    let top = 0.5
                    let left = 0.5
                    let screenAspect = self.frame.size.width / self.frame.size.height
                    let imageAspect = Double(image.width) / Double(image.height)
                    var sx: Double
                    var sy: Double
                    var sw: Double
                    var sh: Double
                    
                    if screenAspect > imageAspect {
                        let ratio = self.frame.size.width * window.screen.scale / Double(image.width)
                        
                        sx = 0.0
                        sh = self.frame.size.height * window.screen.scale / ratio
                        sy = max(0.0, min(Double(image.height) - sh, (Double(image.height) * ratio - self.frame.size.height * window.screen.scale) / ratio * top - self.tracker.movement.y * window.screen.scale / ratio))
                        sw = Double(image.width)
                        
                        let insetTop = (Double(image.height) * ratio - self.frame.size.height * window.screen.scale) * top / window.screen.scale
                        let insetBottom = (self.frame.size.height * window.screen.scale - Double(image.height) * ratio + (Double(image.height) * ratio - self.frame.size.height * window.screen.scale) * top) / window.screen.scale
                        
                        if insetTop < self.tracker.movement.y {
                            if self.tracker.active {
                                self.tracker.edge = true
                            } else if !self.tracker.edge {
                                self.tracker.velocity.y = -abs(self.tracker.velocity.y)
                            }
                            
                            self.tracker.movement.y = insetTop
                        } else if insetBottom > self.tracker.movement.y {
                            if self.tracker.active {
                                self.tracker.edge = true
                            } else if !self.tracker.edge {
                                self.tracker.velocity.y = abs(self.tracker.velocity.y)
                            }
                            
                            self.tracker.movement.y = insetBottom
                        } else if insetTop == self.tracker.movement.y || insetBottom == self.tracker.movement.y {
                            if self.tracker.active {
                                self.tracker.edge = true
                            }
                        } else {
                            self.tracker.edge = false
                        }
                    } else {
                        let ratio = self.frame.size.height * window.screen.scale / Double(image.height)
                        
                        sw = self.frame.size.width * window.screen.scale / ratio
                        sx = max(0.0, min(Double(image.width) - sw, (Double(image.width) * ratio - self.frame.size.width * window.screen.scale) / ratio * left - self.tracker.movement.x * window.screen.scale / ratio))
                        sy = 0.0
                        sh = Double(image.height)
                        
                        let insetLeft = (Double(image.width) * ratio - self.frame.size.width * window.screen.scale) * left / window.screen.scale
                        let insetRight = (self.frame.size.width * window.screen.scale - Double(image.width) * ratio + (Double(image.width) * ratio - self.frame.size.width * window.screen.scale) * left) / window.screen.scale
                        
                        if insetLeft < self.tracker.movement.x {
                            if self.tracker.active {
                                self.tracker.edge = true
                            } else if !self.tracker.edge {
                                self.tracker.velocity.x = -abs(self.tracker.velocity.x)
                            }
                            
                            self.tracker.movement.x = insetLeft
                        } else if insetRight > self.tracker.movement.x {
                            if self.tracker.active {
                                self.tracker.edge = true
                            } else if !self.tracker.edge {
                                self.tracker.velocity.x = abs(self.tracker.velocity.x)
                            }
                            
                            self.tracker.movement.x = insetRight
                        } else if insetLeft == self.tracker.movement.x || insetRight == self.tracker.movement.x {
                            if self.tracker.active {
                                self.tracker.edge = true
                            }
                        } else {
                            self.tracker.edge = false
                        }
                    }
                    
                    let rect = CGRect(x: round(sx), y: round(sy), width: floor(sw), height: floor(sh))
                    
                    if updateRequired, let imageLayer = self.imageLayer {
                        let scaleX = self.frame.size.width * window.screen.scale / rect.size.width
                        let scaleY = self.frame.size.height * window.screen.scale / rect.size.height
                        let size = CGSize(width: floor(Double(image.width) * scaleX / window.screen.scale), height: floor(Double(image.height) * scaleY / window.screen.scale))
                        var i: CGImage? = nil
                        
                        UIGraphicsBeginImageContextWithOptions(size, false, 0)
                        
                        if let context = UIGraphicsGetCurrentContext() {
                            context.interpolationQuality = .high
                            context.setAllowsAntialiasing(true)
                            context.clear(CGRect(origin: CGPoint.zero, size: size))
                            context.translateBy(x: 0.0, y: size.height)
                            context.scaleBy(x: 1.0, y: -1.0)
                            context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: size.width, height: size.height))
                            
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            i = context.makeImage()
                            
                            CATransaction.commit()
                        }
                        
                        UIGraphicsEndImageContext()
                        
                        if self.sourceRect.equalTo(rect) {
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            imageLayer.contents = i
                            
                            CATransaction.commit()
                        } else {
                            if let imageLayer = self.imageLayer {
                                CATransaction.begin()
                                CATransaction.setDisableActions(true)
                                
                                imageLayer.frame = CGRect(x: -floor(scaleX * rect.origin.x / window.screen.scale), y: -floor(scaleY * rect.origin.y / window.screen.scale), width: floor(scaleX * Double(image.width) / window.screen.scale), height: floor(scaleY * Double(image.height) / window.screen.scale))
                                imageLayer.contents = i
                                
                                CATransaction.commit()
                            }
                            
                            self.sourceRect = rect
                        }
                    } else if !self.sourceRect.equalTo(rect) {
                        if let imageLayer = self.imageLayer {
                            let scaleX = self.frame.size.width * window.screen.scale / rect.size.width
                            let scaleY = self.frame.size.height * window.screen.scale / rect.size.height
                            
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            imageLayer.frame = CGRect(x: -floor(scaleX * rect.origin.x / window.screen.scale), y: -floor(scaleY * rect.origin.y / window.screen.scale), width: floor(scaleX * Double(image.width) / window.screen.scale), height: floor(scaleY * Double(image.height) / window.screen.scale))
                            
                            CATransaction.commit()
                        }
                        
                        self.sourceRect = rect
                    }
                }
            } else if self.currentTime == nil {
                if let imageLayer = self.imageLayer {
                    CATransaction.begin()
                    CATransaction.setDisableActions(true)
                    
                    imageLayer.frame = CGRect.zero
                    imageLayer.contents = nil
                    
                    CATransaction.commit()
                }
                
                self.currentTime = 0.0
            }
            
            if !self.particles.isEmpty {
                var imageCache: [Int:(CGImage?, CGSize)] = [:]
                
                for i in stride(from: self.particles.count - 1, through: 0, by: -1) {
                    var particle = self.particles[i]
                    let time = particle.time + deltaTime
                    
                    if time >= particle.delay {
                        let t = particle.time - particle.delay
                        
                        if t >= particle.duration {
                            if let layer = particle.layer {
                                layer.removeFromSuperlayer()
                            }
                            
                            self.particles.remove(at: i)
                            
                            continue
                        } else {
                            let step = sin(t / particle.duration * Double.pi)
                            
                            if let layer = particle.layer {
                                CATransaction.begin()
                                CATransaction.setDisableActions(true)
                                
                                layer.opacity = Float(step)
                                layer.transform = CATransform3DMakeScale(step, step, 1.0)
                                
                                CATransaction.commit()
                            } else {
                                let index = Int.random(in: 0..<8)
                                let layer = CALayer()
                                
                                if let (generatedImage, imageSize) = imageCache[index] {
                                    CATransaction.begin()
                                    CATransaction.setDisableActions(true)
                                    
                                    layer.frame = CGRect(x: Double.random(in: -imageSize.width / 2..<self.frame.size.width - imageSize.width / 2), y: Double.random(in: -imageSize.height / 2..<self.frame.size.height - imageSize.height / 2), width: imageSize.width, height: imageSize.height)
                                    layer.contents = generatedImage
                                    layer.opacity = Float(step)
                                    layer.transform = CATransform3DMakeScale(step, step, 1.0)
                                    
                                    CATransaction.commit()
                                    
                                    particle.layer = layer
                                    self.layer.addSublayer(layer)
                                } else {
                                    let sourceImage = UIImage(named: "Sparkle\(index % 4 + 1)")!
                                    var maskImage: CGImage? = nil
                                    var generatedImage: CGImage? = nil
                                    
                                    UIGraphicsBeginImageContextWithOptions(CGSize(width: sourceImage.size.width, height: sourceImage.size.height), false, sourceImage.scale)
                                    
                                    if let context = UIGraphicsGetCurrentContext(), let cgImage = sourceImage.cgImage {
                                        context.interpolationQuality = .high
                                        context.setAllowsAntialiasing(true)
                                        context.clear(CGRect(x: 0.0, y: 0.0, width: sourceImage.size.width, height: sourceImage.size.height))
                                        context.translateBy(x: 0.0, y: sourceImage.size.height)
                                        context.scaleBy(x: 1.0, y: -1.0)
                                        context.draw(cgImage, in: CGRect(x: 0.0, y: 0.0, width: sourceImage.size.width, height: sourceImage.size.height))
                                        maskImage = context.makeImage()
                                    }
                                    
                                    UIGraphicsEndImageContext()
                                    
                                    if let maskImage {
                                        UIGraphicsBeginImageContextWithOptions(CGSize(width: sourceImage.size.width, height: sourceImage.size.height), false, sourceImage.scale)
                                        
                                        if let context = UIGraphicsGetCurrentContext() {
                                            context.interpolationQuality = .high
                                            context.setAllowsAntialiasing(true)
                                            context.clear(CGRect(x: 0.0, y: 0.0, width: sourceImage.size.width, height: sourceImage.size.height))
                                            context.translateBy(x: 0.0, y: sourceImage.size.height)
                                            context.scaleBy(x: 1.0, y: -1.0)
                                            context.clip(to: CGRect(x: 0.0, y: 0.0, width: sourceImage.size.width, height: sourceImage.size.height), mask: maskImage)
                                            context.setFillColor(CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: index % 2 == 0 ? [0.0, 0.0, 0.0, 1.0] : [1.0, 1.0, 1.0, 1.0])!)
                                            context.fill([CGRect(x: 0.0, y: 0.0, width: sourceImage.size.width, height: sourceImage.size.height)])
                                            
                                            generatedImage = context.makeImage()
                                            imageCache[index] = (generatedImage, sourceImage.size)
                                        }
                                        
                                        UIGraphicsEndImageContext()
                                    }
                                    
                                    CATransaction.begin()
                                    CATransaction.setDisableActions(true)
                                    
                                    layer.frame = CGRect(x: Double.random(in: -sourceImage.size.width / 2..<self.frame.size.width - sourceImage.size.width / 2), y: Double.random(in: -sourceImage.size.height / 2..<self.frame.size.height - sourceImage.size.height / 2), width: sourceImage.size.width, height: sourceImage.size.height)
                                    layer.contents = generatedImage
                                    layer.opacity = Float(step)
                                    layer.transform = CATransform3DMakeScale(step, step, 1.0)
                                    
                                    CATransaction.commit()
                                    
                                    particle.layer = layer
                                    
                                    self.layer.addSublayer(layer)
                                }
                            }
                            
                            particle.time = time
                        }
                    } else {
                        particle.time = time
                    }
                    
                    self.particles[i] = particle
                }
            }
        }
    }
    
    private func createFramesetter(font: CTFont, color: CGColor, text: String) -> CTFramesetter {
        let minimumLineHeight: CGFloat = 0
        let maximumLineHeight: CGFloat = ceil(CTFontGetAscent(font) + CTFontGetDescent(font) + CTFontGetLeading(font))
        let paragraphSpacing: CGFloat = 0
        let paragraphSpacingBefore: CGFloat = 0
        let maximumLineSpacing: CGFloat = 0
        let minimumLineSpacing: CGFloat = 0
        let lineSpacingAdjustment: CGFloat = 0
        let alignment: CTTextAlignment = .left
        let paragraphStyle: CTParagraphStyle = withUnsafeBytes(of: minimumLineHeight) { minimumLineHeightBytes in
            withUnsafeBytes(of: maximumLineHeight) { maximumLineHeightBytes in
                withUnsafeBytes(of: paragraphSpacing) { paragraphSpacingBytes in
                    withUnsafeBytes(of: paragraphSpacingBefore) { paragraphSpacingBeforeBytes in
                        withUnsafeBytes(of: maximumLineSpacing) { maximumLineSpacingBytes in
                            withUnsafeBytes(of: minimumLineSpacing) { minimumLineSpacingBytes in
                                withUnsafeBytes(of: lineSpacingAdjustment) { lineSpacingAdjustmentBytes in
                                    withUnsafeBytes(of: alignment) { alignmentBytes in
                                        CTParagraphStyleCreate([
                                            CTParagraphStyleSetting(spec: .minimumLineHeight, valueSize: MemoryLayout<CGFloat>.size, value: minimumLineHeightBytes.baseAddress!),
                                            CTParagraphStyleSetting(spec: .maximumLineHeight, valueSize: MemoryLayout<CGFloat>.size, value: maximumLineHeightBytes.baseAddress!),
                                            CTParagraphStyleSetting(spec: .paragraphSpacing, valueSize: MemoryLayout<CGFloat>.size, value: paragraphSpacingBytes.baseAddress!),
                                            CTParagraphStyleSetting(spec: .paragraphSpacingBefore, valueSize: MemoryLayout<CGFloat>.size, value: paragraphSpacingBeforeBytes.baseAddress!),
                                            CTParagraphStyleSetting(spec: .maximumLineSpacing, valueSize: MemoryLayout<CGFloat>.size, value: maximumLineSpacingBytes.baseAddress!),
                                            CTParagraphStyleSetting(spec: .minimumLineSpacing, valueSize: MemoryLayout<CGFloat>.size, value: minimumLineSpacingBytes.baseAddress!),
                                            CTParagraphStyleSetting(spec: .lineSpacingAdjustment, valueSize: MemoryLayout<CGFloat>.size, value: lineSpacingAdjustmentBytes.baseAddress!),
                                            CTParagraphStyleSetting(spec: .alignment, valueSize: MemoryLayout<CTTextAlignment>.size, value: alignmentBytes.baseAddress!)
                                        ], 8)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        if let preferredLanguage = Locale.preferredLanguages.first, let languageCode = Locale(identifier: preferredLanguage).language.languageCode {
            return CTFramesetterCreateWithAttributedString(NSAttributedString(string: text, attributes:[NSAttributedString.Key.font: font, NSAttributedString.Key.foregroundColor: color, NSAttributedString.Key.paragraphStyle: paragraphStyle, NSAttributedString.Key.languageIdentifier: languageCode]))
        }
        
        return CTFramesetterCreateWithAttributedString(NSAttributedString(string: text, attributes:[NSAttributedString.Key.font: font, NSAttributedString.Key.foregroundColor: color, NSAttributedString.Key.paragraphStyle: paragraphStyle]))
    }
    
    private func createTextPath(frame: CTFrame) -> CGPath {
        let lines = CTFrameGetLines(frame)
        let lineCount = CFArrayGetCount(lines)
        var lineOrigins = [CGPoint](repeating: CGPoint.zero, count: lineCount)
        let mutablePath = CGMutablePath()
        
        CTFrameGetLineOrigins(frame, CFRange(location: 0, length: lineCount), &lineOrigins)
        
        for lineIndex in 0..<lineCount {
            let line = unsafeBitCast(CFArrayGetValueAtIndex(lines, lineIndex), to: CTLine.self)
            let runs = CTLineGetGlyphRuns(line)
            let runCount = CFArrayGetCount(runs)
            
            for runIndex in 0..<runCount {
                let run = unsafeBitCast(CFArrayGetValueAtIndex(runs, runIndex), to: CTRun.self)
                var ascent: CGFloat = 0
                var descent: CGFloat = 0
                let glyphCount = CTRunGetGlyphCount(run)
                var glyphs: [CGGlyph] = Array(repeating: 0, count: glyphCount)
                var positions: [CGPoint] = Array(repeating: CGPoint.zero, count: glyphCount)
                let font = unsafeBitCast(CFDictionaryGetValue(CTRunGetAttributes(run), Unmanaged.passUnretained(kCTFontAttributeName).toOpaque()), to: CTFont.self)
                
                CTRunGetTypographicBounds(run, CFRange(location: 0, length: CTRunGetGlyphCount(run)), &ascent, &descent, nil)
                CTRunGetGlyphs(run, CFRangeMake(0, glyphCount), &glyphs)
                CTRunGetPositions(run, CFRangeMake(0, glyphCount), &positions)
                
                for glyphIndex in 0..<Int(glyphCount) {
                    if let path = CTFontCreatePathForGlyph(font, glyphs[glyphIndex], nil) {
                        mutablePath.addPath(path, transform: CGAffineTransform(translationX: floor(lineOrigins[lineIndex].x) + positions[glyphIndex].x, y: floor(-lineOrigins[lineIndex].y - ascent + descent + positions[glyphIndex].y)))
                    }
                }
            }
        }
        
        return mutablePath
    }
    
    private nonisolated func choice(probabilities: [Double]) -> Int {
        let random = Double.random(in: 0.0..<1.0)
        var sum = 0.0
        var index = 0
        
        for probability in probabilities {
            if sum <= random && random < sum + probability {
                break
            }
            
            sum += probability
            index += 1
        }
        
        return index
    }
    
    private nonisolated func rgbToHsv(red: Double, green: Double, blue: Double) -> (Double, Double, Double) {
        let max = max(max(red, green), blue)
        let min = min(min(red, green), blue)
        let sub = max - min
        var hue = 0.0
        var saturation = 0.0
        
        if sub > 0.0 {
            if max == red {
                hue = (60.0 * (green - blue) / sub)
            } else if max == green {
                hue = (60.0 * (blue - red) / sub) + 120.0
            } else if max == blue {
                hue = (60.0 * (red - green) / sub) + 240.0
            }
            
            if hue < 0.0 {
                hue += 360
            }
        }
        
        if max > 0.0 {
            saturation = sub / max
        }
        
        return (hue: hue, saturation: saturation, value: max)
    }
    
    private nonisolated func hsvToRgb(hue: Double, saturation: Double, value: Double) -> (Double, Double, Double) {
        if saturation == 0 {
            return (value, value, value)
        }
        
        var red = 0.0
        var green = 0.0
        var blue = 0.0
        let h = fmod(hue, 360.0)
        let s = min(1.0, max(0.0, saturation))
        let v = min(1.0, max(0.0, value))
        let hi = Int(h / 60.0)
        let f = (h / 60.0) - Double(hi)
        let p = v * (1.0 - s)
        let q = v * (1.0 - f * s)
        let t = v * (1.0 - (1.0 - f) * s)
        
        if hi == 0 {
            red = v
            green = t
            blue = p
        } else if hi == 1 {
            red = q
            green = v
            blue = p
        } else if hi == 2 {
            red = p
            green = v
            blue = t
        } else if hi == 3 {
            red = p
            green = q
            blue = v
        } else if hi == 4 {
            red = t
            green = p
            blue = v
        } else if hi == 5 {
            red = v
            green = p
            blue = q
        }
        
        return (red: red, green: green, blue: blue)
    }
    
    private nonisolated func fBm(x: Double, y: Double, z: Double, octaves: Int = 4, persistence: Double = 0.5) -> Double {
        // fractional Brownian motion
        var total = 0.0
        var frequency = 1.0
        var amplitude = 1.0
        var maximum = 0.0
        
        for _ in 0..<octaves {
            total += self.noise(x: x * frequency, y: y * frequency, z: z * frequency) * amplitude
            maximum += amplitude
            amplitude *= persistence
            frequency *= 2.0
        }
        
        return total / maximum
    }
    
    private nonisolated func noise(x: Double, y: Double = 0.0, z: Double = 0.0) -> Double {
        // Perlin noise
        let xi = Int(x) & 255
        let yi = Int(y) & 255
        let zi = Int(z) & 255
        let xf = x - floor(x)
        let yf = y - floor(y)
        let zf = z - floor(z)
        let u = self.fade(t: xf)
        let v = self.fade(t: yf)
        let w = self.fade(t: zf)
        let aaa = self.permutation[(self.permutation[(self.permutation[xi] + yi) % self.permutation.count] + zi) % self.permutation.count]
        let aba = self.permutation[(self.permutation[(self.permutation[xi] + yi + 1) % self.permutation.count] + zi) % self.permutation.count]
        let aab = self.permutation[(self.permutation[(self.permutation[xi] + yi) % self.permutation.count] + zi + 1) % self.permutation.count]
        let abb = self.permutation[(self.permutation[(self.permutation[xi] + yi + 1) % self.permutation.count] + zi + 1) % self.permutation.count]
        let baa = self.permutation[(self.permutation[(self.permutation[(xi + 1) % self.permutation.count] + yi) % self.permutation.count] + zi) % self.permutation.count]
        let bba = self.permutation[(self.permutation[(self.permutation[(xi + 1) % self.permutation.count] + yi + 1) % self.permutation.count] + zi) % self.permutation.count]
        let bab = self.permutation[(self.permutation[(self.permutation[(xi + 1) % self.permutation.count] + yi) % self.permutation.count] + zi + 1) % self.permutation.count]
        let bbb = self.permutation[(self.permutation[(self.permutation[(xi + 1) % self.permutation.count] + yi + 1) % self.permutation.count] + zi + 1) % self.permutation.count]
        var x1 = self.lerp(a: self.grad(hash: aaa, x: xf, y: yf, z: zf), b: self.grad(hash: baa, x: xf-1, y: yf, z: zf), t: u)
        var x2 = self.lerp(a: self.grad(hash: aba, x: xf, y: yf - 1, z: zf), b: self.grad(hash: bba, x: xf - 1, y: yf - 1, z: zf), t: u)
        let y1 = self.lerp(a: x1, b: x2, t: v)
        
        x1 = self.lerp(a: self.grad(hash: aab, x: xf, y: yf, z: zf - 1), b: self.grad(hash: bab, x: xf - 1, y: yf, z: zf - 1), t: u)
        x2 = self.lerp(a: self.grad(hash: abb, x: xf, y: yf - 1, z: zf-1), b: self.grad(hash: bbb, x: xf - 1, y: yf - 1, z: zf - 1), t: u)
        
        let y2 = self.lerp(a: x1, b: x2, t: v)
        
        return (self.lerp(a: y1, b: y2, t: w) + 1.0) / 2.0
    }
    
    private nonisolated func grad(hash: Int, x: Double, y: Double, z: Double) -> Double {
        switch hash & 0xF {
        case 0x0:
            return x + y
        case 0x1:
            return -x + y
        case 0x2:
            return x - y
        case 0x3:
            return -x - y
        case 0x4:
            return x + z
        case 0x5:
            return -x + z
        case 0x6:
            return x - z
        case 0x7:
            return -x - z
        case 0x8:
            return y + z
        case 0x9:
            return -y + z
        case 0xA:
            return y - z
        case 0xB:
            return -y - z
        case 0xC:
            return y + x
        case 0xD:
            return -y + z
        case 0xE:
            return y - x
        case 0xF:
            return -y - z
        default:
            return 0
        }
    }
    
    private nonisolated func fade(t: Double) -> Double {
        return t * t * t * (t * (t * 6 - 15) + 10) // 6t^5 - 15t^4 + 10t^3
    }
    
    private nonisolated func lerp(a: Double, b: Double, t: Double) -> Double {
        return a + t * (b - a)
    }
}

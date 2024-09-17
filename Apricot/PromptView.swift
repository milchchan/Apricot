//
//  Prompt.swift
//  Apricot
//
//  Created by Milch on 2023/01/01.
//

import Foundation
import CoreText
import UIKit
import SwiftUI

class PromptView: UIView {
    var font: UIFont? = nil
    var isScrambled = false
    var scrambleLetters: Set<Character>? = nil
    private var block: (running: Bool, time: Double, duration: Double, type: (elapsed: Double, speed: Double, reverse: Bool, buffer: String, count: Int), text: String, attributes: [(start: Int, end: Int)], current: String, color: (CGColor?, CGColor?), x: Double, width: Double, elapsed: Double) = (running: false, time: 0.0, duration: -1.0, type: (elapsed: -1.0, speed: 50.0, reverse: false, buffer: String(), count: 0), text: String(), attributes: [], current: String(), color: (nil, nil), x: 0.0, width: 0.0, elapsed: 0.0)
    private var line: String? = nil
    private var foregroundColor: CGColor? = nil
    private var accentColor: UIColor? = nil
    private var isInvalidated = false
    private var isReloading = false
    private var underline: Double? = nil
    
    var accent: UIColor {
        get {
            return self.accentColor ?? UIColor(named: "AccentColor")!
        }
        set {
            self.accentColor = newValue
        }
    }
    
    override init(frame: CGRect) {
        super.init(frame: frame)
        
        self.foregroundColor = CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: self.traitCollection.userInterfaceStyle == .dark ? [1.0, 1.0, 1.0, 1.0] : [0.0, 0.0, 0.0, 1.0])
        self.backgroundColor = .clear
        self.contentMode = .scaleAspectFit
        self.registerForTraitChanges([UITraitUserInterfaceStyle.self], handler: { (self: Self, previousTraitCollection: UITraitCollection) in
            if previousTraitCollection.userInterfaceStyle != self.traitCollection.userInterfaceStyle {
                if self.traitCollection.userInterfaceStyle == .dark {
                    self.foregroundColor = CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [1.0, 1.0, 1.0, 1.0])
                } else {
                    self.foregroundColor = CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [0.0, 0.0, 0.0, 1.0])
                }
                
                self.isReloading = true
                self.isInvalidated = true
            }
        })
        
        let displayLink = CADisplayLink(target: self, selector: #selector(self.update))
        
        displayLink.add(to: .current, forMode: .common)
    }
    
    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
    }
    
    func reload(text: String?) {
        self.isReloading = true
        self.isInvalidated = true
        self.line = text
    }
    
    @objc private func update(displayLink: CADisplayLink) {
        if self.frame.size.width > 0 && self.frame.size.height > 0 {
            let deltaTime = displayLink.targetTimestamp - displayLink.timestamp
            
            if self.isInvalidated {
                if self.block.running {
                    self.block.type.reverse = true
                }
                
                self.isInvalidated = false
            }
            
            if !self.block.running && self.block.type.elapsed < 0.0 && !self.block.type.reverse {
                self.block.text.removeAll()
                self.block.attributes.removeAll()
                self.block.current.removeAll()
                self.block.color = (self.foregroundColor, (self.accentColor ?? UIColor(named: "AccentColor")!).cgColor)
                
                if let line = self.line {
                    self.block.running = true
                    self.block.text.append(line)
                    self.block.attributes.append((start: 0, end: line.count))
                    self.block.x = 0.0
                }
            }
            
            if self.block.running {
                var updateRequired = false
                
                if self.block.type.reverse {
                    if self.block.type.count > 0 {
                        self.block.type.elapsed += deltaTime * 2.0
                        
                        if self.block.type.elapsed >= 1.0 / self.block.type.speed {
                            if self.block.type.count - 1 < self.block.text.count {
                                let width = self.block.text.count / 2
                                
                                if self.block.type.buffer.count <= width && self.block.type.count > 0 {
                                    self.block.type.count -= 1
                                }
                                
                                if !self.block.type.buffer.isEmpty {
                                    self.block.type.buffer.remove(at: self.block.type.buffer.index(self.block.type.buffer.endIndex, offsetBy: -1))
                                }
                            }
                            
                            self.block.type.elapsed = 0.0
                        }
                        
                        self.block.elapsed += deltaTime
                    } else {
                        self.block.time = 0.0
                        self.block.type.elapsed = -1.0
                        self.block.type.reverse = false
                        self.block.type.buffer.removeAll()
                        self.block.current.removeAll()
                        self.block.running = false
                        self.block.elapsed = 0.0
                        updateRequired = true
                    }
                } else if self.block.type.buffer.count < self.block.text.count {
                    if self.block.type.elapsed >= 0.0 {
                        self.block.type.elapsed += deltaTime
                    } else {
                        self.block.type.elapsed = deltaTime
                    }
                    
                    if self.block.type.elapsed >= 1.0 / self.block.type.speed {
                        if !self.isScrambled && self.block.type.count >= self.block.text.count / 2 {
                            self.block.type.buffer.append(self.block.text[self.block.text.index(self.block.text.startIndex, offsetBy: self.block.type.buffer.count)])
                        }
                        
                        if self.block.type.count < self.block.text.count {
                            self.block.type.count += 1
                        }
                        
                        self.block.type.elapsed = 0.0
                    }
                    
                    self.block.elapsed += deltaTime
                } else {
                    self.block.time += deltaTime
                    
                    if self.block.duration >= 0.0 && self.block.time >= self.block.duration {
                        self.block.type.reverse = true
                    }
                    
                    self.block.elapsed += deltaTime
                }
                
                if self.block.text.count == self.block.type.buffer.count {
                    self.block.current.removeAll()
                    self.block.current.append(self.block.text)
                    updateRequired = true
                } else {
                    var characters: Set<Character>
                    var randomBuffer = String()
                    
                    if let scrambleLetters = self.scrambleLetters {
                        if scrambleLetters.isEmpty {
                            characters = []
                            
                            for k in 0..<self.block.text.count {
                                let character = self.block.text[self.block.text.index(self.block.text.startIndex, offsetBy: k)]
                                
                                if !characters.contains(character) && !character.isNewline && !character.isWhitespace {
                                    characters.insert(character)
                                }
                            }
                        } else {
                            characters = scrambleLetters
                        }
                    } else {
                        characters = []
                        
                        for k in 0..<self.block.text.count {
                            let character = self.block.text[self.block.text.index(self.block.text.startIndex, offsetBy: k)]
                            
                            if !characters.contains(character) && !character.isNewline && !character.isWhitespace {
                                characters.insert(character)
                            }
                        }
                    }
                    
                    if !characters.isEmpty {
                        for k in 0..<self.block.type.count {
                            let character = self.block.text[self.block.text.index(self.block.text.startIndex, offsetBy: k)]
                            
                            if character.isNewline {
                                randomBuffer.append(character)
                            } else {
                                randomBuffer.append(characters[characters.index(characters.startIndex, offsetBy: Int.random(in: 0..<characters.count))])
                            }
                        }
                    }
                    
                    if randomBuffer.count > self.block.type.buffer.count {
                        self.block.current.removeAll()
                        self.block.current.append(self.block.type.buffer)
                        self.block.current.append(String(randomBuffer[randomBuffer.index(randomBuffer.startIndex, offsetBy: block.type.buffer.count)..<randomBuffer.index(randomBuffer.startIndex, offsetBy: randomBuffer.count)]))
                        updateRequired = true
                    } else if block.current.count != self.block.type.buffer.count {
                        self.block.current.removeAll()
                        self.block.current.append(self.block.type.buffer)
                        updateRequired = true
                    }
                }
                
                if let f = self.font, let color1 = self.block.color.0, let color2 = self.block.color.1 {
                    typealias Segment = (text: String, highlight: Bool, framesetter: CTFramesetter?, size: CGSize)
                    let lineHeight = self.frame.size.height
                    let font = CTFontCreateWithFontDescriptor(f.fontDescriptor as CTFontDescriptor, f.pointSize, nil)
                    let margin = ceil(f.pointSize / 2.0)
                    var current: [Segment] = []
                    var target: [(text: String, highlight: Bool, size: CGSize)] = []
                    var index = 0
                    var width = 0.0
                    var maxWidth = 0.0
                    var translation: Double
                    
                    while index < self.block.current.count {
                        if let attribute = self.block.attributes.first(where: { index >= $0.start && index < $0.end }) {
                            if attribute.end <= self.block.current.count {
                                current.append((text: String(self.block.current[self.block.current.index(self.block.current.startIndex, offsetBy: index)..<self.block.current.index(self.block.current.startIndex, offsetBy: attribute.end)]), highlight: true, framesetter: nil, size: CGSize.zero))
                                index = attribute.end
                            } else {
                                current.append((text: String(self.block.current[self.block.current.index(self.block.current.startIndex, offsetBy: index)..<self.block.current.index(self.block.current.startIndex, offsetBy: self.block.current.count)]), highlight: true, framesetter: nil, size: CGSize.zero))
                                
                                break
                            }
                        } else {
                            var minimum: (start: Int?, distance: Int) = (start: nil, distance: Int.max)
                            
                            for attribute in self.block.attributes {
                                let distance = attribute.start - index
                                
                                if distance >= 0 && distance < minimum.distance {
                                    minimum.distance = distance
                                    minimum.start = attribute.start
                                }
                            }
                            
                            if let start = minimum.start {
                                if start <= self.block.current.count {
                                    current.append((text: String(self.block.current[self.block.current.index(self.block.current.startIndex, offsetBy: index)..<self.block.current.index(self.block.current.startIndex, offsetBy: start)]), highlight: false, framesetter: nil, size: CGSize.zero))
                                    index = start
                                } else {
                                    current.append((text: String(block.current[self.block.current.index(self.block.current.startIndex, offsetBy: index)..<self.block.current.index(self.block.current.startIndex, offsetBy: self.block.current.count)]), highlight: false, framesetter: nil, size: CGSize.zero))
                                    
                                    break
                                }
                            } else {
                                current.append((text: String(self.block.current[self.block.current.index(self.block.current.startIndex, offsetBy: index)..<self.block.current.index(self.block.current.startIndex, offsetBy: self.block.current.count)]), highlight: false, framesetter: nil, size: CGSize.zero))
                                
                                break
                            }
                        }
                    }
                    
                    for j in 0..<current.count {
                        var segment = current[j]
                        let framesetter = self.createFramesetter(font: font, color: segment.highlight ? color2 : color1, text: segment.text)
                        var frameSize = CTFramesetterSuggestFrameSizeWithConstraints(framesetter, CFRange(), nil, CGSize(width: CGFloat.greatestFiniteMagnitude, height: CGFloat.greatestFiniteMagnitude), nil)
                        var spaces = 0
                        
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
                        
                        width += ceil(frameSize.width)
                        segment.framesetter = framesetter
                        segment.size = frameSize
                        current[j] = segment
                    }
                    
                    index = 0
                    
                    while index < self.block.text.count {
                        if let attribute = self.block.attributes.first(where: { index >= $0.start && index < $0.end }) {
                            target.append((text: String(self.block.text[self.block.text.index(self.block.text.startIndex, offsetBy: index)..<self.block.text.index(self.block.text.startIndex, offsetBy: attribute.end)]), highlight: true, size: CGSize.zero))
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
                                target.append((text: String(self.block.text[self.block.text.index(self.block.text.startIndex, offsetBy: index)..<self.block.text.index(self.block.text.startIndex, offsetBy: start)]), highlight: false, size: CGSize.zero))
                                index = start
                            } else {
                                target.append((text: String(self.block.text[self.block.text.index(self.block.text.startIndex, offsetBy: index)..<self.block.text.index(self.block.text.startIndex, offsetBy: self.block.text.count)]), highlight: false, size: CGSize.zero))
                                
                                break
                            }
                        }
                    }
                    
                    for j in 0..<target.count {
                        var segment = target[j]
                        var frameSize = CTFramesetterSuggestFrameSizeWithConstraints(self.createFramesetter(font: font, color: segment.highlight ? color2 : color1, text: segment.text), CFRange(), nil, CGSize(width: CGFloat.greatestFiniteMagnitude, height: CGFloat.greatestFiniteMagnitude), nil)
                        var spaces = 0
                        
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
                        
                        maxWidth += ceil(frameSize.width)
                        segment.size = frameSize
                        target[j] = segment
                    }
                    
                    if self.block.x == 0.0 {
                        self.block.x = width / 2.0
                    }
                    
                    if width / 2.0 > self.block.x {
                        let speed = self.block.type.reverse ? self.block.type.speed * 2.0 : self.block.type.speed
                        
                        self.block.x = min(self.block.x + width / 2.0 * deltaTime * speed, width / 2.0)
                        updateRequired = true
                    } else if width / 2.0 < self.block.x {
                        let speed = self.block.type.reverse ? self.block.type.speed * 2.0 : self.block.type.speed
                        
                        self.block.x = max(self.block.x - width / 2.0 * deltaTime * speed, width / 2.0)
                        updateRequired = true
                    }
                    
                    if self.underline != nil {
                        let length = width - self.block.width
                        
                        if length > 0 {
                            let speed = self.block.type.reverse ? self.block.type.speed * 2.0 : self.block.type.speed
                            
                            self.block.width = min(self.block.width + width * deltaTime * speed, width)
                            updateRequired = true
                        } else if length < 0 {
                            let speed = self.block.type.reverse ? self.block.type.speed * 2.0 : self.block.type.speed
                            
                            self.block.width = max(self.block.width - width * deltaTime * speed, width)
                            updateRequired = true
                        }
                    }
                    
                    if maxWidth > self.frame.size.width {
                        translation = fmod(self.block.elapsed, 10.0) / 10.0 * -(maxWidth + margin)
                        updateRequired = true
                    } else {
                        translation = 0.0
                    }
                    
                    if updateRequired {
                        UIGraphicsBeginImageContextWithOptions(self.frame.size, false, 0)
                        
                        if let context = UIGraphicsGetCurrentContext() {
                            var offset = 0.0
                            
                            context.interpolationQuality = .high
                            context.setAllowsAntialiasing(true)
                            context.clear(CGRect(origin: CGPoint.zero, size: self.frame.size))
                            context.translateBy(x: 0, y: self.frame.size.height)
                            context.scaleBy(x: 1.0, y: -1.0)
                            context.translateBy(x: self.frame.size.width / 2.0 - self.block.x, y: 0.0)
                            
                            if let underline = self.underline {
                                context.setFillColor(color1)
                                context.fill([CGRect(x: 0.0, y: 0.0, width: self.block.width, height: underline)])
                            }
                            
                            context.clip(to: [CGRect(x: 0.0, y: 0.0, width: max(width, maxWidth), height: lineHeight)])
                            context.translateBy(x: translation, y: 0.0)
                            
                            for _ in 0..<2 {
                                var x = 0.0
                                
                                for segment in current {
                                    if Double(self.frame.size.width) / 2.0 - self.block.x + translation + offset + x + segment.size.width >= 0 && Double(self.frame.size.width) / 2.0 - self.block.x + translation + offset + x < self.frame.size.width {
                                        if segment.highlight {
                                            context.setFillColor(color2)
                                        } else {
                                            context.setFillColor(color1)
                                        }
                                        
                                        self.drawText(context: context, location: CGPoint(x: offset + round(x), y: round(lineHeight - (lineHeight - segment.size.height) / 2.0)), frame: CTFramesetterCreateFrame(segment.framesetter!, CFRange(), CGPath(rect: CGRect(origin: CGPoint.zero, size: segment.size), transform: nil), nil))
                                    }
                                    
                                    x += ceil(segment.size.width)
                                }
                                
                                for segment in target {
                                    offset += ceil(segment.size.width)
                                }
                                
                                offset += margin
                            }
                            
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            self.layer.contents = context.makeImage()
                            
                            CATransaction.commit()
                        }
                        
                        UIGraphicsEndImageContext()
                    }
                }
            }
        }
    }
    
    func createFramesetter(font: CTFont, color: CGColor, text: String) -> CTFramesetter {
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
    
    func drawText(context: CGContext, location: CGPoint, frame: CTFrame) {
        let lines = CTFrameGetLines(frame)
        let lineCount = CFArrayGetCount(lines)
        var lineOrigins = [CGPoint](repeating: CGPoint.zero, count: lineCount)
        
        CTFrameGetLineOrigins(frame, CFRange(location: 0, length: lineCount), &lineOrigins)
        
        context.saveGState()
        context.textMatrix = .identity
        
        for index in 0..<lineCount {
            let line = unsafeBitCast(CFArrayGetValueAtIndex(lines, index), to: CTLine.self)
            var ascent: CGFloat = 0
            var descent: CGFloat = 0
            
            CTLineGetTypographicBounds(line, &ascent, &descent, nil)
            
            context.saveGState()
            context.translateBy(x: location.x + floor(lineOrigins[index].x), y: location.y - ceil(lineOrigins[index].y + ascent - descent))
            
            CTLineDraw(line, context)
            
            context.restoreGState()
        }
        
        context.restoreGState()
    }
}

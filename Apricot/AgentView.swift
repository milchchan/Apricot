//
//  AgentView.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation
import AVFoundation
import CoreLocation
import CoreImage
import ImageIO
import UniformTypeIdentifiers
import VideoToolbox
import UIKit
import WidgetKit

@MainActor
protocol AgentDelegate: AnyObject {
    func agentShouldIdle(_ agent: AgentView, by name: String) -> Bool
    func agentWillSpeak(_ agent: AgentView, message: Message)
    func agentDidStart(_ agent: AgentView)
    func agentDidRender(_ agent: AgentView, image: CGImage, by name: String)
    func agentDidRefresh(_ agent: AgentView)
    func agentDidTransition(_ agent: AgentView)
    func agentDidStop(_ agent: AgentView)
    func agentDidChange(_ agent: AgentView, successfully flag: Bool)
    func agentDidUpdate(_ agent: AgentView, background: [[(url: URL?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)]]?)
}

class AgentView: UIView, @MainActor CAAnimationDelegate, @MainActor AVAudioPlayerDelegate {
    weak var delegate: (any AgentDelegate)? = nil
    var characterViews = [CharacterView]()
    var attributes = [String]()
    private(set) var path = String()
    private var displayLink: CADisplayLink? = nil
    private var frameTimestamps = [CFTimeInterval]()
    private var audioPlayer: AVAudioPlayer? = nil
    private var accentColor: UIColor? = nil
    private var userScale = 1.0
    private var systemScale = 1.0
    private var guest: String? = nil
    private var isMute = false
    private var isRunning = true
    private var revision: (UInt64, UInt64) = (0, 0)
    private var stars = 0
    private var snapshot: ([Sprite], CGImage?) = ([], nil)
    var types: [(String, Bool)] {
        return self.characterViews.reduce(into: [], { x, y in
            if y.name != self.guest {
                for (key, value) in y.types.sorted(by: { $0.value.0 < $1.value.0 }) {
                    if !x.contains(where: { $0.0 == key }) {
                        x.append((key, value.1))
                    }
                }
            }
        })
    }
    var accent: UIColor {
        get {
            return self.accentColor ?? UIColor(named: "AccentColor")!
        }
        set {
            self.accentColor = newValue
        }
    }
    var mute: Bool {
        get {
            return self.isMute
        }
        set {
            self.isMute = newValue
            
            if newValue {
                if let audioPlayer = self.audioPlayer, audioPlayer.isPlaying {
                    audioPlayer.volume = 0.0
                }
                
                for characterView in self.characterViews {
                    if let audioPlayer = characterView.audioPlayer, audioPlayer.isPlaying {
                        audioPlayer.volume = 0.0
                    }
                }
            } else {
                if let audioPlayer = self.audioPlayer, audioPlayer.isPlaying {
                    audioPlayer.volume = 1.0
                }
                
                for characterView in self.characterViews {
                    if let audioPlayer = characterView.audioPlayer, audioPlayer.isPlaying {
                        audioPlayer.volume = 1.0
                    }
                }
            }
        }
    }
    var running: Bool {
        return self.isRunning
    }
    var idle: Bool {
        return self.characterViews.allSatisfy({ $0.lastIdleDate != nil })
    }
    
    private override init(frame: CGRect) {
        super.init(frame: frame)
        
        self.backgroundColor = .clear
        self.isOpaque = false
        self.isUserInteractionEnabled = true
        self.clipsToBounds = false
    }
    
    convenience init(path: String, types: Int, scale: Double, stars: Int) {
        var characters = [(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, upscaling: Bool, language: String?, prompt: String?, guest: Bool, sequences: [Sequence], types: [String: (Int, Set<Int>)], insets: (top: Double, left: Double, bottom: Double, right: Double))]()
        
        self.init(frame: .zero)
        self.path = path
        self.userScale = scale
        self.stars = stars
        
        for filename in Script.resolve(directory: path) {
            let tuple = Script.Parser().parse(path: filename)
            
            for character in tuple.0 {
                if let index = characters.firstIndex(where: { $0.name == character.name }) {
                    characters[index] = (name: character.name, path: filename, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, prompt: character.prompt, guest: false, sequences: character.sequences, types: character.types, insets: character.insets)
                } else {
                    characters.append((name: character.name, path: filename, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, prompt: character.prompt, guest: false, sequences: character.sequences, types: character.types, insets: character.insets))
                }
            }
            
            for attribute in tuple.1 {
                if !self.attributes.contains(attribute) {
                    self.attributes.append(attribute)
                }
            }
        }
        
        self.attributes.sort { $0 < $1 }
        
        if characters.count == 1 {
            var resolvedPaths = [(String, String)]()
            let parser = Script.Parser()
            var languages = [String?]()
            
            parser.excludeSequences = true
            
            if let preferredLanguage = Locale.preferredLanguages.first {
                let components = Locale.Language.Components(identifier: preferredLanguage)
                
                if let languageCode = components.languageCode {
                    if let script = components.script {
                        languages.append("\(languageCode.identifier)-\(script.identifier)")
                    }
                    
                    languages.append(languageCode.identifier)
                }
            }
            
            languages.append(nil)
            
            if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
                let documentsUrl = containerUrl.appending(path: "Documents", directoryHint: .isDirectory)
                let documentsPath = documentsUrl.path(percentEncoded: false)
                
                if FileManager.default.fileExists(atPath: documentsPath) {
                    var urlQueue: [(URL, String)] = [(documentsUrl, "Documents")]
                    var directories = [String]()
                    
                    repeat {
                        let (baseUrl, basePath) = urlQueue.removeFirst()
                        
                        if let urls = try? FileManager.default.contentsOfDirectory(at: baseUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
                            for url in urls {
                                if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), values.isDirectory ?? false, let name = values.name {
                                    let directory = "\(basePath)/\(name)"
                                    
                                    directories.append(directory)
                                    urlQueue.append((baseUrl.appending(path: name, directoryHint: .isDirectory), directory))
                                }
                            }
                        }
                    } while (!urlQueue.isEmpty)
                    
                    for directory in directories {
                        if let urls = try? FileManager.default.contentsOfDirectory(at: containerUrl.appending(path: directory, directoryHint: .isDirectory), includingPropertiesForKeys: [.nameKey], options: .skipsHiddenFiles) {
                            var paths = [String: [(URL, String, String?, String, String?)]]()
                            
                            for url in urls {
                                if let values = try? url.resourceValues(forKeys: [.nameKey]), let name = values.name, let match = name.wholeMatch(of: /^(.+?)(?:\.([a-z]{2,3}(?:-[A-Z][a-z]{3})?))?\.(?:json|xml)$/) {
                                    let key = String(match.output.1)
                                    let path = url.path(percentEncoded: false)
                                    var characterName: String? = nil
                                    var prompt: String? = nil
                                    
                                    if var tuple = paths[key] {
                                        if let output = match.output.2 {
                                            var languageCode = String(output)
                                            
                                            for character in parser.parse(path: path).0 {
                                                if let language = character.language {
                                                    languageCode = language
                                                }
                                                
                                                characterName = character.name
                                                prompt = character.prompt
                                            }
                                            
                                            if let characterName {
                                                tuple.append((url, directory, String(languageCode), characterName, prompt))
                                            }
                                        } else {
                                            for character in parser.parse(path: path).0 {
                                                characterName = character.name
                                                prompt = character.prompt
                                            }
                                            
                                            if let characterName {
                                                tuple.append((url, directory, nil, characterName, prompt))
                                            }
                                        }
                                        
                                        paths[key] = tuple
                                    } else if let output = match.output.2 {
                                        var languageCode = String(output)
                                        
                                        for character in parser.parse(path: path).0 {
                                            if let language = character.language {
                                                languageCode = language
                                            }
                                            
                                            characterName = character.name
                                            prompt = character.prompt
                                        }
                                        
                                        if let characterName {
                                            paths[key] = [(url, directory, String(languageCode), characterName, prompt)]
                                        }
                                    } else {
                                        for character in parser.parse(path: path).0 {
                                            characterName = character.name
                                            prompt = character.prompt
                                        }
                                        
                                        if let characterName {
                                            paths[key] = [(url, directory, nil, characterName, prompt)]
                                        }
                                    }
                                }
                            }
                            
                            for value in paths.values {
                                var isResolved = false
                                
                                for language in languages {
                                    for tuple in value {
                                        if tuple.2 == language {
                                            if let prompt = tuple.4, prompt.range(of: characters[0].name) != nil {
                                                resolvedPaths.append((tuple.1, tuple.3))
                                            }
                                            
                                            isResolved = true
                                        }
                                    }
                                    
                                    if isResolved {
                                        break
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            for resouce in ["Merku", "Milch"] {
                var paths = [String: [(String, String, String?, String, String?)]]()
                
                for path in Bundle.main.paths(forResourcesOfType: "xml", inDirectory: resouce) {
                    let input = URL(filePath: path).deletingPathExtension().lastPathComponent
                    var characterName: String? = nil
                    var prompt: String? = nil
                    
                    if let match = input.wholeMatch(of: /^(.+?)\.([a-z]{2,3}(?:-[A-Z][a-z]{3})?)$/) {
                        let key = String(match.output.1)
                        var languageCode = String(match.output.2)
                        
                        for character in parser.parse(path: path).0 {
                            if let language = character.language {
                                languageCode = language
                            }
                            
                            characterName = character.name
                            prompt = character.prompt
                        }
                        
                        if let characterName {
                            if var tuple = paths[key] {
                                tuple.append((path, resouce, languageCode, characterName, prompt))
                                paths[key] = tuple
                            } else {
                                paths[key] = [(path, resouce, languageCode, characterName, prompt)]
                            }
                        }
                    } else {
                        for character in parser.parse(path: path).0 {
                            characterName = character.name
                            prompt = character.prompt
                        }
                        
                        if let characterName {
                            if var tuple = paths[input] {
                                tuple.append((path, resouce, nil, characterName, prompt))
                                paths[input] = tuple
                            } else {
                                paths[input] = [(path, resouce, nil, characterName, prompt)]
                            }
                        }
                    }
                }
                
                for value in paths.values {
                    var isResolved = false
                    
                    for language in languages {
                        for tuple in value {
                            if tuple.2 == language {
                                if !resolvedPaths.contains(where: { $0.1 == tuple.3 }), let prompt = tuple.4, prompt.range(of: characters[0].name) != nil {
                                    resolvedPaths.append((tuple.1, tuple.3))
                                }
                                
                                isResolved = true
                            }
                        }
                        
                        if isResolved {
                            break
                        }
                    }
                }
            }
            
            for i in stride(from: resolvedPaths.count - 1, through: 0, by: -1) {
                if characters.contains(where: { $0.name == resolvedPaths[i].1 }) {
                    resolvedPaths.remove(at: i)
                }
            }
            
            if !resolvedPaths.isEmpty {
                let (path, name) = resolvedPaths[Int.random(in: 0..<resolvedPaths.count)]
                
                for filename in Script.resolve(directory: path) {
                    for character in Script.Parser().parse(path: filename).0 {
                        if character.name == name {
                            characters.append((name: character.name, path: filename, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, prompt: character.prompt, guest: true, sequences: character.sequences, types: character.types, insets: character.insets))
                            self.guest = character.name
                            
                            break
                        }
                    }
                }
            }
        }
        
        var keys = [(String, Bool)]()
        let state = String(stars)
        
        for i in 0..<characters.count {
            let character = characters[i]
            let characterView = self.make(name: character.name, path: character.path, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, sequences: character.sequences, types: character.types, insets: character.insets)
            let dateComponents = Calendar.current.dateComponents([.calendar, .timeZone, .era, .year, .month, .day, .hour, .minute], from: Date())
            var animations: [Animation]? = nil
            
            for (key, value) in characterView.types.sorted(by: { $0.value.0 < $1.value.0 }) {
                if let first = keys.first(where: { $0.0 == key }) {
                    if first.1 {
                        characterView.types[key] = (value.0, true, value.2)
                    }
                } else if types & Int(pow(2.0, Double(keys.count))) > 0 {
                    characterView.types[key] = (value.0, true, value.2)
                    keys.append((key, true))
                } else {
                    keys.append((key, false))
                }
            }
            
            if i > 0 {
                characterView.isMirror = true
            }
            
            Script.shared.characters.append((name: character.name, path: character.path, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, prompt: character.prompt, guest: character.guest, sequences: character.sequences))
            
            if let date = dateComponents.date {
                Script.shared.run(name: character.name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                    if y.name == character.name {
                        for sequence in y.sequences {
                            if sequence.name == "Tick" {
                                x.append(sequence)
                            }
                        }
                    }
                }), state: ISO8601DateFormatter.string(from: date, timeZone: .current, formatOptions: [.withFullDate, .withTime, .withDashSeparatorInDate, .withColonSeparatorInTime])) { _ in [] }
            }
            
            Script.shared.run(name: character.name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                if y.name == character.name {
                    for sequence in y.sequences {
                        if sequence.name == "Star" {
                            x.append(sequence)
                        }
                    }
                }
            }), state: state) { _ in [] }
            
            Script.shared.run(name: character.name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                if y.name == character.name {
                    for sequence in y.sequences {
                        if sequence.name == "Start" {
                            x.append(sequence)
                        }
                    }
                }
            })) { x in
                var y = x
                
                animations = x.compactMap({ sequence in
                    for step in sequence {
                        if case .animations(let animations) = step {
                            return animations
                        }
                    }
                    
                    return nil
                }).first
                
                if i == 0 {
                    var sequence = Sequence(name: nil, state: String())
                    
                    for s in x {
                        for step in s {
                            sequence.append(step)
                        }
                    }
                    
                    y.append(sequence)
                }
                
                y.append(Sequence(name: String()))
                
                return y
            }
            
            if let animations {
                let baseUrl = URL(filePath: character.path).deletingLastPathComponent()
                let displayScale = Double(self.traitCollection.displayScale)
                let screenScale = Int(round(displayScale))
                var pathSet = Set<String>()
                var sources = [(baseUrl, false)]
                var selectedPaths = [String: String]()
                var loadedImages = [String: [(CGImage, Bool)]]()
                
                for animation in animations {
                    for sprite in animation {
                        if let path = sprite.path, !path.isEmpty && !pathSet.contains(path) {
                            pathSet.insert(path)
                        }
                    }
                }
                
                if character.upscaling, let documents = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {
                    sources.append((documents.appending(path: baseUrl.lastPathComponent, directoryHint: .isDirectory), true))
                }
                
                for (source, upscaling) in sources {
                    for relativePath in pathSet {
                        let imageUrl = source.appending(path: relativePath, directoryHint: .inferFromPath)
                        var candidates = [relativePath]
                        var image: CGImage? = nil
                        
                        if upscaling {
                            guard let path = selectedPaths[relativePath] else {
                                continue
                            }
                            
                            candidates = [path]
                        } else if screenScale > 1 {
                            let name = imageUrl.lastPathComponent[imageUrl.lastPathComponent.startIndex..<imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.endIndex, offsetBy: imageUrl.pathExtension.isEmpty ? 0 : -imageUrl.pathExtension.count - 1)]
                            let filename = "\(name)@\(screenScale)x\(imageUrl.lastPathComponent[imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.startIndex, offsetBy: name.count)..<imageUrl.lastPathComponent.endIndex])"
                            
                            candidates.insert(((relativePath as NSString).deletingLastPathComponent as NSString).appendingPathComponent(filename), at: 0)
                        }
                        
                        for candidate in candidates {
                            let path = source.appending(path: candidate, directoryHint: .inferFromPath).path(percentEncoded: false)
                            
                            if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                defer {
                                    try? file.close()
                                }
                                
                                if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                    for i in 0..<CGImageSourceGetCount(imageSource) {
                                        image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                        
                                        break
                                    }
                                }
                            }
                            
                            if image != nil {
                                selectedPaths[relativePath] = candidate
                                
                                break
                            }
                        }
                        
                        if let image {
                            if var value = loadedImages[relativePath] {
                                value.append((image, upscaling))
                                loadedImages[relativePath] = value
                            } else {
                                loadedImages[relativePath] = [(image, upscaling)]
                            }
                        }
                    }
                }
                
                for (key, value) in loadedImages {
                    if let baseImage = value.first(where: { !$0.1 }) {
                        if let upscalingImage = value.first(where: { $0.1 }) {
                            if Double(upscalingImage.0.width) / Double(baseImage.0.width) > displayScale {
                                let width = Int(Double(baseImage.0.width) * displayScale)
                                let height = Int(Double(baseImage.0.height) * displayScale)
                                
                                if let context = CGContext(data: nil, width: width, height: height, bitsPerComponent: 8, bytesPerRow: 0, space: CGColorSpace(name: CGColorSpace.sRGB)!, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) {
                                    
                                    context.interpolationQuality = .high
                                    context.draw(upscalingImage.0, in: CGRect(x: 0, y: 0, width: width, height: height))
                                    characterView.cachedImages[key] = (baseImage.0, context.makeImage())
                                } else {
                                    characterView.cachedImages[key] = (baseImage.0, nil)
                                }
                            } else {
                                characterView.cachedImages[key] = (baseImage.0, upscalingImage.0)
                            }
                        } else {
                            characterView.cachedImages[key] = (baseImage.0, nil)
                        }
                    }
                }
                
                if let sample = characterView.cachedImages.values.first(where: { $0.1 != nil }), let image = sample.1 {
                    characterView.imageScale = Double(image.width) / Double(sample.0.width)
                }
                
                characterView.cachedTimelines.append(contentsOf: animations.map { Timeline(animation: $0) })
            }
            
            self.characterViews.append(characterView)
        }
    }
    
    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
    }
    
    @discardableResult
    func toggle(type: String) -> [(String, Bool)] {
        var types = [(String, Bool)]()
        
        for characterView in self.characterViews {
            if let value1 = characterView.types[type] {
                if value1.1 {
                    if !types.contains(where: { $0.0 == type }) {
                        types.append((type, false))
                    }
                    
                    characterView.types[type] = (value1.0, false, value1.2)
                } else {
                    for (key, value2) in characterView.types {
                        if key != type && value2.1 {
                            if types.contains(where: { $0.0 == key }) {
                                characterView.types[key] = (value2.0, false, value2.2)
                            } else if value1.2.symmetricDifference(value2.2).isEmpty {
                                types.append((key, false))
                                characterView.types[key] = (value2.0, false, value2.2)
                            }
                        }
                    }
                    
                    if !types.contains(where: { $0.0 == type }) {
                        types.append((type, true))
                    }
                    
                    characterView.types[type] = (value1.0, true, value1.2)
                }
                
                characterView.isInvalidated = true
            }
        }
        
        return types
    }
    
    func invalidate() {
        for characterView in self.characterViews {
            characterView.isInvalidated = true
        }
    }
    
    func change(path: String) {
        self.revision.0 &+= 2
        
        let generation = self.revision.0
        
        UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseOut, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
            self.superview?.alpha = 0.0
        }, completion: { [self] finished in
            if self.revision.0 == generation {
                if generation % 2 == 0 {
                    if finished {
                        self.path = path
                        self.revision.0 &+= 1
                        
                        Task {
                            var alpha: Double
                            var interval: Double
                            var offset: Double
                            let (characters, attributes, guest) = await Task.detached {
                                var characters = [(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, upscaling: Bool, language: String?, prompt: String?, guest: Bool, sequences: [Sequence], types: [String: (Int, Set<Int>)], insets: (top: Double, left: Double, bottom: Double, right: Double))]()
                                var attributes = [String]()
                                var guest: String? = nil
                                
                                for p in Script.resolve(directory: path) {
                                    let tuple = Script.Parser().parse(path: p)
                                    
                                    for character in tuple.0 {
                                        if let index = characters.firstIndex(where: { $0.name == character.name }) {
                                            characters[index] = (name: character.name, path: p, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, prompt: character.prompt, guest: false, sequences: character.sequences, types: character.types, insets: character.insets)
                                        } else {
                                            characters.append((name: character.name, path: p, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, prompt: character.prompt, guest: false, sequences: character.sequences, types: character.types, insets: character.insets))
                                        }
                                    }
                                    
                                    for attribute in tuple.1 {
                                        if !attributes.contains(attribute) {
                                            attributes.append(attribute)
                                        }
                                    }
                                }
                                
                                attributes.sort { $0 < $1 }
                                
                                if characters.count == 1 {
                                    var resolvedPaths = [(String, String)]()
                                    let parser = Script.Parser()
                                    var languages = [String?]()
                                    
                                    parser.excludeSequences = true
                                    
                                    if let preferredLanguage = Locale.preferredLanguages.first {
                                        let components = Locale.Language.Components(identifier: preferredLanguage)
                                        
                                        if let languageCode = components.languageCode {
                                            if let script = components.script {
                                                languages.append("\(languageCode.identifier)-\(script.identifier)")
                                            }
                                            
                                            languages.append(languageCode.identifier)
                                        }
                                    }
                                    
                                    languages.append(nil)
                                    
                                    if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
                                        let documentsUrl = containerUrl.appending(path: "Documents", directoryHint: .isDirectory)
                                        let documentsPath = documentsUrl.path(percentEncoded: false)
                                        
                                        if FileManager.default.fileExists(atPath: documentsPath) {
                                            var urlQueue: [(URL, String)] = [(documentsUrl, "Documents")]
                                            var directories = [String]()
                                            
                                            repeat {
                                                let (baseUrl, basePath) = urlQueue.removeFirst()
                                                
                                                if let urls = try? FileManager.default.contentsOfDirectory(at: baseUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
                                                    for url in urls {
                                                        if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), values.isDirectory ?? false, let name = values.name {
                                                            let directory = "\(basePath)/\(name)"
                                                            
                                                            directories.append(directory)
                                                            urlQueue.append((baseUrl.appending(path: name, directoryHint: .isDirectory), directory))
                                                        }
                                                    }
                                                }
                                            } while (!urlQueue.isEmpty)
                                            
                                            for directory in directories {
                                                if let urls = try? FileManager.default.contentsOfDirectory(at: containerUrl.appending(path: directory, directoryHint: .isDirectory), includingPropertiesForKeys: [.nameKey], options: .skipsHiddenFiles) {
                                                    var paths = [String: [(URL, String, String?, String, String?)]]()
                                                    
                                                    for url in urls {
                                                        if let values = try? url.resourceValues(forKeys: [.nameKey]), let name = values.name, let match = name.wholeMatch(of: /^(.+?)(?:\.([a-z]{2,3}(?:-[A-Z][a-z]{3})?))?\.(?:json|xml)$/) {
                                                            let key = String(match.output.1)
                                                            let path = url.path(percentEncoded: false)
                                                            var characterName: String? = nil
                                                            var prompt: String? = nil
                                                            
                                                            if var tuple = paths[key] {
                                                                if let output = match.output.2 {
                                                                    var languageCode = String(output)
                                                                    
                                                                    for character in parser.parse(path: path).0 {
                                                                        if let language = character.language {
                                                                            languageCode = language
                                                                        }
                                                                        
                                                                        characterName = character.name
                                                                        prompt = character.prompt
                                                                    }
                                                                    
                                                                    if let characterName {
                                                                        tuple.append((url, directory, String(languageCode), characterName, prompt))
                                                                    }
                                                                } else {
                                                                    for character in parser.parse(path: path).0 {
                                                                        characterName = character.name
                                                                        prompt = character.prompt
                                                                    }
                                                                    
                                                                    if let characterName {
                                                                        tuple.append((url, directory, nil, characterName, prompt))
                                                                    }
                                                                }
                                                                
                                                                paths[key] = tuple
                                                            } else if let output = match.output.2 {
                                                                var languageCode = String(output)
                                                                
                                                                for character in parser.parse(path: path).0 {
                                                                    if let language = character.language {
                                                                        languageCode = language
                                                                    }
                                                                    
                                                                    characterName = character.name
                                                                    prompt = character.prompt
                                                                }
                                                                
                                                                if let characterName {
                                                                    paths[key] = [(url, directory, String(languageCode), characterName, prompt)]
                                                                }
                                                            } else {
                                                                for character in parser.parse(path: path).0 {
                                                                    characterName = character.name
                                                                    prompt = character.prompt
                                                                }
                                                                
                                                                if let characterName {
                                                                    paths[key] = [(url, directory, nil, characterName, prompt)]
                                                                }
                                                            }
                                                        }
                                                    }
                                                    
                                                    for value in paths.values {
                                                        var isResolved = false
                                                        
                                                        for language in languages {
                                                            for tuple in value {
                                                                if tuple.2 == language {
                                                                    if let prompt = tuple.4, prompt.range(of: characters[0].name) != nil {
                                                                        resolvedPaths.append((tuple.1, tuple.3))
                                                                    }
                                                                    
                                                                    isResolved = true
                                                                }
                                                            }
                                                            
                                                            if isResolved {
                                                                break
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    
                                    for resouce in ["Merku", "Milch"] {
                                        var paths = [String: [(String, String, String?, String, String?)]]()
                                        
                                        for path in Bundle.main.paths(forResourcesOfType: "xml", inDirectory: resouce) {
                                            let input = URL(filePath: path).deletingPathExtension().lastPathComponent
                                            var characterName: String? = nil
                                            var prompt: String? = nil
                                            
                                            if let match = input.wholeMatch(of: /^(.+?)\.([a-z]{2,3}(?:-[A-Z][a-z]{3})?)$/) {
                                                let key = String(match.output.1)
                                                var languageCode = String(match.output.2)
                                                
                                                for character in parser.parse(path: path).0 {
                                                    if let language = character.language {
                                                        languageCode = language
                                                    }
                                                    
                                                    characterName = character.name
                                                    prompt = character.prompt
                                                }
                                                
                                                if let characterName {
                                                    if var tuple = paths[key] {
                                                        tuple.append((path, resouce, languageCode, characterName, prompt))
                                                        paths[key] = tuple
                                                    } else {
                                                        paths[key] = [(path, resouce, languageCode, characterName, prompt)]
                                                    }
                                                }
                                            } else {
                                                for character in parser.parse(path: path).0 {
                                                    characterName = character.name
                                                    prompt = character.prompt
                                                }
                                                
                                                if let characterName {
                                                    if var tuple = paths[input] {
                                                        tuple.append((path, resouce, nil, characterName, prompt))
                                                        paths[input] = tuple
                                                    } else {
                                                        paths[input] = [(path, resouce, nil, characterName, prompt)]
                                                    }
                                                }
                                            }
                                        }
                                        
                                        for value in paths.values {
                                            var isResolved = false
                                            
                                            for language in languages {
                                                for tuple in value {
                                                    if tuple.2 == language {
                                                        if !resolvedPaths.contains(where: { $0.1 == tuple.3 }), let prompt = tuple.4, prompt.range(of: characters[0].name) != nil {
                                                            resolvedPaths.append((tuple.1, tuple.3))
                                                        }
                                                        
                                                        isResolved = true
                                                    }
                                                }
                                                
                                                if isResolved {
                                                    break
                                                }
                                            }
                                        }
                                    }
                                    
                                    for i in stride(from: resolvedPaths.count - 1, through: 0, by: -1) {
                                        if characters.contains(where: { $0.name == resolvedPaths[i].1 }) {
                                            resolvedPaths.remove(at: i)
                                        }
                                    }
                                    
                                    if !resolvedPaths.isEmpty {
                                        let (path, name) = resolvedPaths[Int.random(in: 0..<resolvedPaths.count)]
                                        
                                        for filename in Script.resolve(directory: path) {
                                            for character in Script.Parser().parse(path: filename).0 {
                                                if character.name == name {
                                                    characters.append((name: character.name, path: filename, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, prompt: character.prompt, guest: true, sequences: character.sequences, types: character.types, insets: character.insets))
                                                    guest = character.name
                                                    
                                                    break
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                if let cachesUrl = FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask).first, let urls = try? FileManager.default.contentsOfDirectory(at: cachesUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
                                    for url in urls {
                                        if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), let isDirectory = values.isDirectory, !isDirectory, let name = values.name, let match = name.firstMatch(of: /^[0-9a-f]{64}$/), !match.output.isEmpty {
                                            try? FileManager.default.removeItem(atPath: url.path(percentEncoded: false))
                                        }
                                    }
                                }
                                
                                if let containerUrl = FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: "group.com.milchchan.Apricot"), let urls = try? FileManager.default.contentsOfDirectory(at: containerUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
                                    for url in urls {
                                        if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), let isDirectory = values.isDirectory, !isDirectory, let name = values.name, UUID(uuidString: name) != nil {
                                            try? FileManager.default.removeItem(atPath: url.path(percentEncoded: false))
                                        }
                                    }
                                }
                                
                                return (characters, attributes, guest)
                            }.value
                            
                            for i in stride(from: Script.shared.characters.count - 1, through: 0, by: -1) {
                                if !characters.contains(where: { $0.name == Script.shared.characters[i].name }) {
                                    Script.shared.characters.remove(at: i)
                                }
                            }
                            
                            self.attributes.removeAll()
                            self.attributes.append(contentsOf: attributes)
                            
                            for constraint in self.constraints.filter({ constraint in
                                self.characterViews.contains(where: { characterView in
                                    constraint.firstItem === characterView || constraint.firstItem === characterView.balloonView
                                })
                            }) {
                                self.removeConstraint(constraint)
                            }
                            
                            for characterView in self.characterViews {
                                if let gestureRecognizers = characterView.contentView.gestureRecognizers {
                                    for gestureRecognizer in gestureRecognizers {
                                        characterView.contentView.removeGestureRecognizer(gestureRecognizer)
                                    }
                                }
                                
                                if let audioPlayer = characterView.audioPlayer, audioPlayer.isPlaying {
                                    audioPlayer.stop()
                                }
                                
                                characterView.removeFromSuperview()
                                characterView.balloonView!.removeFromSuperview()
                            }
                            
                            self.characterViews.removeAll()
                            
                            if Script.shared.characters.count > characters.count {
                                for i in stride(from: Script.shared.characters.count - 1, to: characters.count - 1, by: -1) {
                                    Script.shared.characters.remove(at: i)
                                }
                            }
                            
                            _ = await Script.shared.update { states in
                                states.removeAll()
                                
                                return true
                            }
                            
                            Script.shared.queue.removeAll()
                            
                            self.snapshot = ([], nil)
                            self.guest = guest
                            
                            let safeBounds = self.bounds.inset(by: self.safeAreaInsets)
                            
                            if safeBounds.width > safeBounds.height && !characters.isEmpty {
                                alpha = 1.0
                                interval = safeBounds.width / Double(characters.count)
                                offset = safeBounds.maxX - interval / 2.0 - self.bounds.midX
                                
                                let maxWidth = characters.reduce(0.0, { max((abs($1.insets.right) - abs($1.insets.left)) * ($1.scale == 0.0 ? self.traitCollection.displayScale : $1.scale) * self.userScale / self.traitCollection.displayScale, $0) })
                                let maxHeight = characters.reduce(0.0, { max((abs($1.insets.bottom) - abs($1.insets.top)) * ($1.scale == 0.0 ? self.traitCollection.displayScale : $1.scale) * self.userScale / self.traitCollection.displayScale, $0) })
                                let horizontalScale = maxWidth > 0.0 ? interval / maxWidth : 1.0
                                let verticalScale = maxHeight > 0.0 ? safeBounds.height / 2.0 / maxHeight : 1.0
                                
                                self.systemScale = min(horizontalScale, verticalScale, 1.0)
                            } else {
                                alpha = 0.0
                                interval = 0.0
                                offset = 0.0
                                
                                self.systemScale = 1.0
                            }
                            
                            let state = String(self.stars)
                            
                            for i in 0..<characters.count {
                                let character = characters[i]
                                let characterView = self.make(name: character.name, path: character.path, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, sequences: character.sequences, types: character.types, insets: character.insets)
                                let dateComponents = Calendar.current.dateComponents([.calendar, .timeZone, .era, .year, .month, .day, .hour, .minute], from: Date())
                                var animations: [Animation]? = nil
                                
                                if i > 0 {
                                    characterView.isMirror = true
                                    characterView.alpha = alpha
                                }
                                
                                characterView.transform.tx = offset - interval * Double(i)
                                
                                self.characterViews.append(characterView)
                                
                                if i < Script.shared.characters.count {
                                    Script.shared.characters[i] = (name: character.name, path: character.path, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, prompt: character.prompt, guest: character.guest, sequences: character.sequences)
                                } else {
                                    Script.shared.characters.append((name: character.name, path: character.path, location: character.location, size: character.size, scale: character.scale, upscaling: character.upscaling, language: character.language, prompt: character.prompt, guest: character.guest, sequences: character.sequences))
                                }
                                
                                if let date = dateComponents.date {
                                    await Script.shared.run(name: character.name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                                        if y.name == character.name {
                                            for sequence in y.sequences {
                                                if sequence.name == "Tick" {
                                                    x.append(sequence)
                                                }
                                            }
                                        }
                                    }), state: ISO8601DateFormatter.string(from: date, timeZone: .current, formatOptions: [.withFullDate, .withTime, .withDashSeparatorInDate, .withColonSeparatorInTime]), words: []) { _ in [] }
                                }
                                
                                await Script.shared.run(name: character.name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                                    if y.name == character.name {
                                        for sequence in y.sequences {
                                            if sequence.name == "Star" {
                                                x.append(sequence)
                                            }
                                        }
                                    }
                                }), state: state, words: []) { _ in [] }
                                
                                await Script.shared.run(name: character.name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                                    if y.name == character.name {
                                        for sequence in y.sequences {
                                            if sequence.name == "Start" {
                                                x.append(sequence)
                                            }
                                        }
                                    }
                                }), words: []) { x in
                                    var y = x
                                    
                                    animations = x.compactMap({ sequence in
                                        for step in sequence {
                                            if case .animations(let animations) = step {
                                                return animations
                                            }
                                        }
                                        
                                        return nil
                                    }).first
                                    
                                    if i == 0 {
                                        var sequence = Sequence(name: nil, state: String())
                                        
                                        for s in x {
                                            for step in s {
                                                sequence.append(step)
                                            }
                                        }
                                        
                                        y.append(sequence)
                                    }
                                    
                                    y.append(Sequence(name: String()))
                                    
                                    return y
                                }
                                
                                if let animations {
                                    let baseUrl = URL(filePath: character.path).deletingLastPathComponent()
                                    let displayScale = Double(self.traitCollection.displayScale)
                                    let screenScale = Int(round(displayScale))
                                    let loadedImages = await Task.detached { @Sendable [animations, baseUrl, screenScale, displayScale, upscaling = character.upscaling] in
                                        var pathSet = Set<String>()
                                        var sources = [(baseUrl, false)]
                                        var selectedPaths = [String: String]()
                                        var loadedImages = [String: [(CGImage, Bool)]]()
                                        var cachedImages = [String: (CGImage, CGImage?)]()
                                        
                                        for animation in animations {
                                            for sprite in animation {
                                                if let path = sprite.path, !path.isEmpty && !pathSet.contains(path) {
                                                    pathSet.insert(path)
                                                }
                                            }
                                        }
                                        
                                        if upscaling, let documents = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {
                                            sources.append((documents.appending(path: baseUrl.lastPathComponent, directoryHint: .isDirectory), true))
                                        }
                                        
                                        for (source, upscaling) in sources {
                                            for relativePath in pathSet {
                                                let imageUrl = source.appending(path: relativePath, directoryHint: .inferFromPath)
                                                var candidates = [relativePath]
                                                var image: CGImage? = nil
                                                
                                                if upscaling {
                                                    guard let path = selectedPaths[relativePath] else {
                                                        continue
                                                    }
                                                    
                                                    candidates = [path]
                                                } else if screenScale > 1 {
                                                    let name = imageUrl.lastPathComponent[imageUrl.lastPathComponent.startIndex..<imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.endIndex, offsetBy: imageUrl.pathExtension.isEmpty ? 0 : -imageUrl.pathExtension.count - 1)]
                                                    let filename = "\(name)@\(screenScale)x\(imageUrl.lastPathComponent[imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.startIndex, offsetBy: name.count)..<imageUrl.lastPathComponent.endIndex])"
                                                    
                                                    candidates.insert(((relativePath as NSString).deletingLastPathComponent as NSString).appendingPathComponent(filename), at: 0)
                                                }
                                                
                                                for candidate in candidates {
                                                    let path = source.appending(path: candidate, directoryHint: .inferFromPath).path(percentEncoded: false)
                                                    
                                                    if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                                        defer {
                                                            try? file.close()
                                                        }
                                                        
                                                        if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                                            for i in 0..<CGImageSourceGetCount(imageSource) {
                                                                image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                                                
                                                                break
                                                            }
                                                        }
                                                    }
                                                    
                                                    if image != nil {
                                                        selectedPaths[relativePath] = candidate
                                                        
                                                        break
                                                    }
                                                }
                                                
                                                if let image {
                                                    if var value = loadedImages[relativePath] {
                                                        value.append((image, upscaling))
                                                        loadedImages[relativePath] = value
                                                    } else {
                                                        loadedImages[relativePath] = [(image, upscaling)]
                                                    }
                                                }
                                            }
                                        }
                                        
                                        for (key, value) in loadedImages {
                                            if let baseImage = value.first(where: { !$0.1 }) {
                                                if let upscalingImage = value.first(where: { $0.1 }) {
                                                    if Double(upscalingImage.0.width) / Double(baseImage.0.width) > displayScale {
                                                        let width = Int(Double(baseImage.0.width) * displayScale)
                                                        let height = Int(Double(baseImage.0.height) * displayScale)
                                                        
                                                        if let context = CGContext(data: nil, width: width, height: height, bitsPerComponent: 8, bytesPerRow: 0, space: CGColorSpace(name: CGColorSpace.sRGB)!, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) {
                                                            
                                                            context.interpolationQuality = .high
                                                            context.draw(upscalingImage.0, in: CGRect(x: 0, y: 0, width: width, height: height))
                                                            cachedImages[key] = (baseImage.0, context.makeImage())
                                                        } else {
                                                            cachedImages[key] = (baseImage.0, nil)
                                                        }
                                                    } else {
                                                        cachedImages[key] = (baseImage.0, upscalingImage.0)
                                                    }
                                                } else {
                                                    cachedImages[key] = (baseImage.0, nil)
                                                }
                                            }
                                        }
                                        
                                        if let sample = cachedImages.values.first(where: { $0.1 != nil }), let image = sample.1 {
                                            return (cachedImages, Double(image.width) / Double(sample.0.width))
                                        }
                                        
                                        return (cachedImages, 1.0)
                                    }.value
                                    
                                    characterView.cachedImages = loadedImages.0
                                    characterView.imageScale = loadedImages.1
                                    
                                    let timelines = animations.map { Timeline(animation: $0) }
                                    let images: [String: CGImage]
                                    let imageScale: Double
                                    
                                    if characterView.imageScale > 1.0 && characterView.cachedImages.values.allSatisfy({ $0.1 != nil }) {
                                        images = characterView.cachedImages.mapValues { $0.1! }
                                        imageScale = characterView.imageScale
                                    } else {
                                        images = characterView.cachedImages.mapValues { $0.0 }
                                        imageScale = 1.0
                                    }
                                    
                                    let (image, fades) = characterView.preview(timelines: timelines, images: images, imageScale: imageScale)
                                    
                                    if let image {
                                        let actualScale = self.userScale * self.systemScale
                                        let imageScale = (character.scale == 0.0 ? 1.0 : character.scale / self.traitCollection.displayScale) * actualScale
                                        let imageSize = CGSize(width: ceil(character.size.width * imageScale), height: ceil(character.size.height * imageScale))
                                        let format = UIGraphicsImageRendererFormat(for: self.traitCollection)
                                        
                                        format.opaque = false
                                        format.preferredRange = .standard
                                        format.scale = self.traitCollection.displayScale
                                        
                                        let renderer = UIGraphicsImageRenderer(size: imageSize, format: format)
                                        let renderedImage = renderer.image { rendererContext in
                                            let context = rendererContext.cgContext
                                            
                                            if actualScale == floor(actualScale) {
                                                context.interpolationQuality = .none
                                                context.setAllowsAntialiasing(false)
                                            } else {
                                                context.interpolationQuality = .high
                                                context.setAllowsAntialiasing(true)
                                            }
                                            
                                            context.clear(CGRect(origin: CGPoint.zero, size: imageSize))
                                            
                                            if characterView.isMirror {
                                                context.translateBy(x: imageSize.width, y: imageSize.height)
                                                context.scaleBy(x: -1.0, y: -1.0)
                                            } else {
                                                context.translateBy(x: 0, y: imageSize.height)
                                                context.scaleBy(x: 1.0, y: -1.0)
                                            }
                                            
                                            context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: imageSize.width, height: imageSize.height))
                                        }
                                        
                                        if let image = renderedImage.cgImage {
                                            CATransaction.begin()
                                            CATransaction.setDisableActions(true)
                                            
                                            characterView.contentView.layer.contents = image
                                            
                                            CATransaction.commit()
                                        }
                                        
                                        for (key, value) in fades {
                                            characterView.fades[key] = value
                                        }
                                    }
                                }
                            }
                            
                            await AgentView.Upscaler.shared.run(characters: Script.shared.characters) { @MainActor [weak self] total in
                                guard total > 0 else {
                                    return
                                }

                                Task.detached {
                                    let image = UIImage(systemName: "sparkles", withConfiguration: UIImage.SymbolConfiguration(font: .systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .caption1).pointSize, weight: .bold)))!

                                    await MainActor.run {
                                        if let self, let characterView = self.characterViews.first {
                                            self.notify(characterView: characterView, image: image, text: nil, duration: 5.0)
                                        }
                                    }
                                }
                            }
                            
                            WidgetCenter.shared.reloadAllTimelines()
                            
                            self.isRunning = true
                            self.revision.0 &+= 1
                            
                            UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseIn, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
                                self.superview?.alpha = 1.0
                            })
                            
                            self.delegate?.agentDidChange(self, successfully: true)
                        }
                    } else {
                        self.isRunning = true
                        
                        UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseIn, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
                            self.superview?.alpha = 1.0
                        })
                        
                        self.delegate?.agentDidChange(self, successfully: false)
                    }
                } else {
                    self.delegate?.agentDidChange(self, successfully: false)
                }
            }
        })
    }
    
    func change(scale: Double) {
        self.revision.1 &+= 1
        
        let generation = self.revision.1
        
        UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseOut, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
            self.alpha = 0.0
        }, completion: { finished in
            if self.revision.1 == generation {
                if finished {
                    self.userScale = scale
                    
                    for characterView in self.characterViews {
                        let preferredScale = (characterView.scale == 0.0 ? self.traitCollection.displayScale : characterView.scale) * scale * self.systemScale
                        let frame = CGRect(x: characterView.origin.x * preferredScale / self.traitCollection.displayScale, y: characterView.origin.y * preferredScale / self.traitCollection.displayScale, width: characterView.size.width * preferredScale / self.traitCollection.displayScale, height: characterView.size.height * preferredScale / self.traitCollection.displayScale)
                        let messageWidth = characterView.constraints.reduce(0.0, { $1.firstItem === characterView.balloonView && $1.firstAttribute == .width ? $1.constant : $0 })
                        let maxScale = messageWidth > 0.0 ? (messageWidth + 16.0) / messageWidth : 0.0
                        let balloonHeight = characterView.constraints.reduce(0.0, { $1.firstItem === characterView.balloonView && $1.firstAttribute == .height ? $1.constant : $0 })
                        let horizontalPadding = round((characterView.contentInsets.leading + characterView.contentInsets.trailing) * preferredScale / self.traitCollection.displayScale / 2.0)
                        let verticalPadding = round((characterView.contentInsets.top + characterView.contentInsets.bottom) * preferredScale / self.traitCollection.displayScale / 2.0)
                        
                        for motionEffect in characterView.contentView.motionEffects {
                            if let motionEffectGroup = motionEffect as? UIMotionEffectGroup, let motionEffects = motionEffectGroup.motionEffects {
                                for me in motionEffects {
                                    if let interpolatingMotionEffect = me as? UIInterpolatingMotionEffect {
                                        if interpolatingMotionEffect.type == .tiltAlongHorizontalAxis {
                                            interpolatingMotionEffect.minimumRelativeValue = -horizontalPadding
                                            interpolatingMotionEffect.maximumRelativeValue = horizontalPadding
                                        } else if interpolatingMotionEffect.type == .tiltAlongVerticalAxis {
                                            interpolatingMotionEffect.minimumRelativeValue = -verticalPadding
                                            interpolatingMotionEffect.maximumRelativeValue = verticalPadding
                                        }
                                    }
                                }
                            }
                        }
                        
                        for constraint in characterView.constraints {
                            if constraint.firstItem === characterView {
                                if constraint.firstAttribute == .width {
                                    constraint.constant = ceil(max(frame.width, messageWidth * maxScale))
                                } else if constraint.firstAttribute == .height {
                                    constraint.constant = ceil(frame.height + balloonHeight * maxScale - frame.origin.y)
                                }
                            } else if constraint.firstItem === characterView.contentView && constraint.secondItem === characterView {
                                if constraint.firstAttribute == .width {
                                    constraint.constant = -floor(max(frame.width, messageWidth * maxScale) - frame.width)
                                } else if constraint.firstAttribute == .height {
                                    constraint.constant = -floor(balloonHeight * maxScale - frame.origin.y)
                                }
                            } else if constraint.firstItem === characterView.balloonView {
                                if constraint.firstAttribute == .width {
                                    constraint.constant = messageWidth
                                } else if constraint.firstAttribute == .bottom {
                                    constraint.constant = round(balloonHeight / 2.0 - frame.origin.y)
                                }
                            }
                        }
                        
                        if !characterView.cachedTimelines.isEmpty {
                            let images: [String: CGImage]
                            let imageScale: Double
                            
                            if characterView.imageScale > 1.0 && characterView.cachedImages.values.allSatisfy({ $0.1 != nil }) {
                                images = characterView.cachedImages.mapValues { $0.1! }
                                imageScale = characterView.imageScale
                            } else {
                                images = characterView.cachedImages.mapValues { $0.0 }
                                imageScale = 1.0
                            }
                            
                            let (image, _) = characterView.preview(timelines: characterView.cachedTimelines, images: images, imageScale: imageScale)
                            
                            if let image {
                                let actualScale = scale * self.systemScale
                                let imageScale = (characterView.scale == 0.0 ? 1.0 : characterView.scale / self.traitCollection.displayScale) * actualScale
                                let imageSize = CGSize(width: ceil(characterView.size.width * imageScale), height: ceil(characterView.size.height * imageScale))
                                let format = UIGraphicsImageRendererFormat(for: self.traitCollection)
                                
                                format.opaque = false
                                format.preferredRange = .standard
                                format.scale = self.traitCollection.displayScale
                                
                                let renderer = UIGraphicsImageRenderer(size: imageSize, format: format)
                                let renderedImage = renderer.image { rendererContext in
                                    let context = rendererContext.cgContext
                                    
                                    if actualScale == floor(actualScale) {
                                        context.interpolationQuality = .none
                                        context.setAllowsAntialiasing(false)
                                    } else {
                                        context.interpolationQuality = .high
                                        context.setAllowsAntialiasing(true)
                                    }
                                    
                                    context.clear(CGRect(origin: CGPoint.zero, size: imageSize))
                                    
                                    if characterView.isMirror {
                                        context.translateBy(x: imageSize.width, y: imageSize.height)
                                        context.scaleBy(x: -1.0, y: -1.0)
                                    } else {
                                        context.translateBy(x: 0, y: imageSize.height)
                                        context.scaleBy(x: 1.0, y: -1.0)
                                    }
                                    
                                    context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: imageSize.width, height: imageSize.height))
                                }
                                
                                if let image = renderedImage.cgImage {
                                    CATransaction.begin()
                                    CATransaction.setDisableActions(true)
                                    
                                    characterView.contentView.layer.contents = image
                                    
                                    CATransaction.commit()
                                }
                            }
                        }
                    }
                }
                
                UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseIn, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
                    self.alpha = 1.0
                })
            }
        })
    }
    
    func notify(characterView: CharacterView, image: UIImage, text: String?, duration: Double, action: (() -> Void)? = nil) {
        let time = characterView.subviews.reduce(CACurrentMediaTime()) { time, subview in
            if let animation = subview.layer.animation(forKey: "notify") {
                return max(time, animation.beginTime + animation.duration)
            }
            
            return time
        }
        let button = UIButton(type: .system)
        var configuration = UIButton.Configuration.glass()
        let font = UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .footnote).pointSize, weight: .bold)
        let length = font.lineHeight + 16.0
        
        configuration.cornerStyle = .capsule
        
        if let text {
            var attributes: [NSAttributedString.Key: Any] = [.font: font]
            
            if let language = characterView.language {
                attributes[.languageIdentifier] = language
            }
            
            configuration.imagePadding = 8.0
            configuration.contentInsets = NSDirectionalEdgeInsets(top: (length - max(font.lineHeight, image.size.height)) / 2.0, leading: (length + 16.0 - image.size.width) / 2.0, bottom: (length - max(font.lineHeight, image.size.height)) / 2.0, trailing: (length + 16.0 - image.size.width) / 2.0)
            configuration.attributedTitle = AttributedString(NSAttributedString(string: text, attributes: attributes))
        } else {
            configuration.imagePadding = 0.0
            configuration.contentInsets = NSDirectionalEdgeInsets(top: (length - image.size.height) / 2.0, leading: (length - image.size.width) / 2.0, bottom: (length - image.size.height) / 2.0, trailing: (length - image.size.width) / 2.0)
        }
        
        button.translatesAutoresizingMaskIntoConstraints = false
        button.isUserInteractionEnabled = false
        
        if let action {
            let color = self.accentColor ?? UIColor(named: "AccentColor")!
            
            configuration.image = image.withTintColor(color, renderingMode: .alwaysOriginal)
            configuration.baseForegroundColor = color
            
            button.isExclusiveTouch = true
            button.configuration = configuration
            button.addAction(UIAction { [weak self, weak button, weak characterView] _ in
                guard let self, let button, let characterView, button.superview === characterView else {
                    return
                }
                
                action()
                
                let opacity = button.layer.presentation()?.opacity ?? button.layer.opacity
                let transform = button.layer.presentation()?.transform ?? button.layer.transform
                let animationGroup = CAAnimationGroup()
                let opacityAnimation = CABasicAnimation(keyPath: "opacity")
                let transformAnimation = CABasicAnimation(keyPath: "transform")
                
                button.isUserInteractionEnabled = false
                
                opacityAnimation.fromValue = opacity
                opacityAnimation.toValue = 0.0
                opacityAnimation.timingFunction = CAMediaTimingFunction(name: .easeIn)
                
                transformAnimation.fromValue = transform
                transformAnimation.toValue = CATransform3DMakeScale(1.5, -1.5, 1.0)
                transformAnimation.timingFunction = CAMediaTimingFunction(name: .easeIn)
                
                animationGroup.beginTime = CACurrentMediaTime()
                animationGroup.duration = 1.0
                animationGroup.isRemovedOnCompletion = false
                animationGroup.fillMode = .forwards
                animationGroup.delegate = self
                animationGroup.animations = [opacityAnimation, transformAnimation]
                
                button.layer.add(animationGroup, forKey: "notify")
            }, for: .touchUpInside)
            
            let delay = max(0.0, time - CACurrentMediaTime())
            
            DispatchQueue.main.asyncAfter(deadline: .now() + delay) { [weak button, weak characterView] in
                guard let button, let characterView, button.superview === characterView else {
                    return
                }
                
                button.isUserInteractionEnabled = true
            }
        } else {
            configuration.image = image.withTintColor(.label, renderingMode: .alwaysOriginal)
            configuration.baseForegroundColor = .label
            
            button.configuration = configuration
        }
        
        button.layer.transform = CATransform3DMakeScale(1.5, -1.5, 1.0)
        characterView.insertSubview(button, belowSubview: characterView.balloonView!)
        characterView.addConstraint(NSLayoutConstraint(item: button, attribute: .centerX, relatedBy: .equal, toItem: characterView, attribute: .centerX, multiplier: 1.0, constant: 0.0))
        characterView.addConstraint(NSLayoutConstraint(item: button, attribute: .bottom, relatedBy: .equal, toItem: characterView, attribute: .bottom, multiplier: 1.0, constant: button.systemLayoutSizeFitting(UIView.layoutFittingCompressedSize).height))
        
        CATransaction.begin()
        
        let animationGroup = CAAnimationGroup()
        let keyframeAnimation1 = CAKeyframeAnimation(keyPath: "opacity")
        let keyframeAnimation2 = CAKeyframeAnimation(keyPath: "transform")
        
        keyframeAnimation1.keyTimes = [0.0, NSNumber(floatLiteral: 1.0 / (duration + 2.0)), NSNumber(floatLiteral: 1.0 - 1.0 / (duration + 2.0)), 1.0]
        keyframeAnimation1.values = [0.0, 1.0, 1.0, 0.0]
        keyframeAnimation1.timingFunctions = [CAMediaTimingFunction(name: .easeOut), CAMediaTimingFunction(name: .linear), CAMediaTimingFunction(name: .easeIn)]
        
        keyframeAnimation2.keyTimes = [0.0, NSNumber(floatLiteral: 1.0 / (duration + 2.0)), NSNumber(floatLiteral: 1.0 - 1.0 / (duration + 2.0)), 1.0]
        keyframeAnimation2.values = [CATransform3DMakeScale(1.5, -1.5, 1.0), CATransform3DMakeScale(1.0, -1.0, 1.0), CATransform3DMakeScale(1.0, -1.0, 1.0), CATransform3DMakeScale(1.5, -1.5, 1.0)]
        keyframeAnimation2.timingFunctions = [CAMediaTimingFunction(name: .easeOut), CAMediaTimingFunction(name: .linear), CAMediaTimingFunction(name: .easeIn)]
        
        animationGroup.beginTime = time
        animationGroup.duration = duration + 2.0
        animationGroup.isRemovedOnCompletion = false
        animationGroup.fillMode = .both
        animationGroup.delegate = self
        animationGroup.animations = [keyframeAnimation1, keyframeAnimation2]
        
        button.layer.add(animationGroup, forKey: "notify")
        
        CATransaction.commit()
    }
    
    func update(stars: Int) {
        let prior = self.stars
        let state = String(stars)
        
        self.stars = stars
        
        Task { [self] in
            var background: [[(url: URL?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)]]? = nil
            
            if prior < stars {
                for (index, characterView) in self.characterViews.enumerated() {
                    if index > 0 {
                        await Script.shared.run(name: characterView.name!, sequences: Script.shared.characters.reduce(into: [], { x, y in
                            if y.name == characterView.name {
                                for sequence in y.sequences {
                                    if sequence.name == "Star" {
                                        x.append(sequence)
                                    }
                                }
                            }
                        }), state: state, words: []) { sequences in
                            var tempSequences = [Sequence]()
                            var cachedAnimations = [Int: [UInt]]()
                            let types = characterView.types.compactMap({ $0.value.1 ? $0.key : nil })
                            
                            for sequence in sequences {
                                var tempSequence = Sequence(name: sequence.name, state: sequence.state)
                                
                                for step in sequence {
                                    if case .animations(let animations) = step {
                                        var tempAnimations2 = [Animation]()
                                        
                                        for animation in animations {
                                            var tempTypes: [String?]? = nil
                                            let isVisible: Bool
                                            
                                            if animation.type == nil {
                                                tempTypes = []
                                                
                                                for timeline in characterView.cachedTimelines {
                                                    if timeline.animation.z == animation.z {
                                                        tempTypes!.append(timeline.animation.type)
                                                    }
                                                }
                                            }
                                            
                                            if let tempTypes {
                                                if types.isEmpty {
                                                    isVisible = true
                                                } else {
                                                    isVisible = !tempTypes.contains { type in
                                                        if let type {
                                                            return types.contains(type)
                                                        }
                                                        
                                                        return false
                                                    }
                                                }
                                            } else if types.isEmpty {
                                                isVisible = false
                                            } else {
                                                tempTypes = []
                                                
                                                for timeline in characterView.cachedTimelines {
                                                    if let type = timeline.animation.type, timeline.animation.z == animation.z && types.contains(type) {
                                                        tempTypes!.append(type)
                                                    }
                                                }
                                                
                                                isVisible = !tempTypes!.isEmpty && tempTypes!.lastIndex(of: animation.type!) == tempTypes!.count - 1
                                            }
                                            
                                            if isVisible && animation.z < 0 {
                                                var group = cachedAnimations[animation.z] ?? []
                                                
                                                group.append(animation.repeats)
                                                cachedAnimations[animation.z] = group
                                            } else {
                                                tempAnimations2.append(animation)
                                            }
                                        }
                                        
                                        tempSequence.append(.animations(tempAnimations2))
                                    } else {
                                        tempSequence.append(step)
                                    }
                                }
                                
                                tempSequences.append(tempSequence)
                            }
                            
                            if !cachedAnimations.isEmpty && cachedAnimations.allSatisfy({ $0.value.count == 1 && $0.value[0] == 0 }) {
                                return tempSequences
                            }
                            
                            return sequences
                        }
                    } else {
                        var unlockedAchievements = [String]()
                        var frames = [[(url: URL?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)]]()
                        
                        for character in Script.shared.characters {
                            if character.name == characterView.name {
                                var sequences = [Sequence]()
                                
                                for sequence in character.sequences {
                                    if sequence.name == "Star" {
                                        sequences.append(sequence)
                                    }
                                }
                                
                                for name in (await Task.detached { @Sendable [sequences, prior, stars] in
                                    var names = [String]()
                                    
                                    if prior < stars {
                                        for sequence in sequences {
                                            if let pattern = sequence.state, let regex = try? Regex(pattern) {
                                                if "\(prior)".firstMatch(of: regex) == nil {
                                                    for i in prior + 1...stars {
                                                        if "\(i)".firstMatch(of: regex) != nil {
                                                            for j in 0..<sequence.count {
                                                                if case .sequence(let s1) = sequence[j] {
                                                                    if let name = s1.name, s1.state == nil && !s1.isEmpty {
                                                                        for k in j + 1..<sequence.count {
                                                                            if case .sequence(let s2) = sequence[k] {
                                                                                var isAvailable = false
                                                                                
                                                                                if s2.isEmpty {
                                                                                    if s1.name == s2.name && s2.state == nil {
                                                                                        isAvailable = true
                                                                                    }
                                                                                } else {
                                                                                    var queue = [Sequence]()
                                                                                    
                                                                                    for step in s2 {
                                                                                        if case .sequence(let s3) = step {
                                                                                            queue.append(s3)
                                                                                        }
                                                                                    }
                                                                                    
                                                                                    while !queue.isEmpty {
                                                                                        let s = queue.removeFirst()
                                                                                        
                                                                                        if s.isEmpty {
                                                                                            if s1.name == s.name && s.state == nil {
                                                                                                isAvailable = true
                                                                                            }
                                                                                        } else {
                                                                                            for step in s {
                                                                                                if case .sequence(let s3) = step {
                                                                                                    queue.append(s3)
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                                
                                                                                if isAvailable {
                                                                                    if !names.contains(where: { $0 == name }) {
                                                                                        names.append(name)
                                                                                    }
                                                                                    
                                                                                    break
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            
                                                            break
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    
                                    return names
                                }.value) {
                                    unlockedAchievements.append(name)
                                }
                                
                                let scale = characterView.scale == 0.0 ? self.traitCollection.displayScale : characterView.scale
                                
                                await Script.shared.run(name: character.name, sequences: sequences, state: state, words: []) { sequences in
                                    let baseUrl = URL(filePath: characterView.path!).deletingLastPathComponent()
                                    var tempSequences = [Sequence]()
                                    var cachedAnimations = [Int: [(UInt, [(url: URL?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)])]]()
                                    var minZIndex = Int.max
                                    let types = characterView.types.compactMap({ $0.value.1 ? $0.key : nil })
                                    
                                    for sequence in sequences {
                                        var tempSequence = Sequence(name: sequence.name, state: sequence.state)
                                        
                                        for step in sequence {
                                            if case .animations(let animations) = step {
                                                var tempAnimations2 = [Animation]()
                                                
                                                for animation in animations {
                                                    var tempTypes: [String?]? = nil
                                                    let isVisible: Bool
                                                    
                                                    if animation.type == nil {
                                                        tempTypes = []
                                                        
                                                        for timeline in characterView.cachedTimelines {
                                                            if timeline.animation.z == animation.z {
                                                                tempTypes!.append(timeline.animation.type)
                                                            }
                                                        }
                                                    }
                                                    
                                                    if let tempTypes {
                                                        if types.isEmpty {
                                                            isVisible = true
                                                        } else {
                                                            isVisible = !tempTypes.contains { type in
                                                                if let type {
                                                                    return types.contains(type)
                                                                }
                                                                
                                                                return false
                                                            }
                                                        }
                                                    } else if types.isEmpty {
                                                        isVisible = false
                                                    } else {
                                                        tempTypes = []
                                                        
                                                        for timeline in characterView.cachedTimelines {
                                                            if let type = timeline.animation.type, timeline.animation.z == animation.z && types.contains(type) {
                                                                tempTypes!.append(type)
                                                            }
                                                        }
                                                        
                                                        isVisible = !tempTypes!.isEmpty && tempTypes!.lastIndex(of: animation.type!) == tempTypes!.count - 1
                                                    }
                                                    
                                                    if isVisible && animation.z < 0 {
                                                        var group = cachedAnimations[animation.z] ?? []
                                                        var images = [(url: URL?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)]()
                                                        
                                                        for sprite in animation {
                                                            let url: URL?
                                                            
                                                            if let path = sprite.path {
                                                                if path.lowercased().hasPrefix("https://") {
                                                                    url = URL(string: path)
                                                                } else {
                                                                    url = baseUrl.appending(path: path, directoryHint: .inferFromPath)
                                                                }
                                                            } else {
                                                                url = nil
                                                            }
                                                            
                                                            images.append((url: url, x: sprite.location.x * scale, y: sprite.location.y * scale, width: sprite.size.width * scale, height: sprite.size.height * scale, opacity: sprite.opacity, delay: sprite.delay))
                                                        }
                                                        
                                                        group.append((animation.repeats, images))
                                                        cachedAnimations[animation.z] = group
                                                        
                                                        if animation.z < minZIndex {
                                                            minZIndex = animation.z
                                                        }
                                                    } else {
                                                        tempAnimations2.append(animation)
                                                    }
                                                }
                                                
                                                tempSequence.append(.animations(tempAnimations2))
                                            } else {
                                                tempSequence.append(step)
                                            }
                                        }
                                        
                                        tempSequences.append(tempSequence)
                                    }
                                    
                                    if !cachedAnimations.isEmpty && cachedAnimations.allSatisfy({ $0.value.count == 1 && $0.value[0].0 == 0 }) {
                                        for i in minZIndex..<0 {
                                            if let images = cachedAnimations[i] {
                                                frames.append(images[0].1)
                                            }
                                        }
                                        
                                        return tempSequences
                                    }
                                    
                                    return sequences
                                }
                                
                                break
                            }
                        }
                        
                        if !unlockedAchievements.isEmpty {
                            Task.detached { [weak self] in
                                let image = UIImage(systemName: "lock.open", withConfiguration: UIImage.SymbolConfiguration(font: .systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .caption1).pointSize, weight: .bold)))!
                                
                                await MainActor.run {
                                    for unlockedAchievement in unlockedAchievements {
                                        self?.notify(characterView: characterView, image: image, text: unlockedAchievement, duration: 5.0)
                                    }
                                }
                            }
                        }
                        
                        background = frames
                    }
                }
                
                if let path = Bundle.main.path(forResource: "Star", ofType: "wav") {
                    Task.detached { [weak self] in
                        if let file = FileHandle(forReadingAtPath: path) {
                            defer {
                                try? file.close()
                            }
                            
                            if let data = try? file.readToEnd(), let audioPlayer = try? AVAudioPlayer(data: data) {
                                let audioSession = AVAudioSession.sharedInstance()
                                var isActivated = true
                                
                                do {
                                    if audioSession.category != .playAndRecord && audioSession.category != .ambient {
                                        try audioSession.setCategory(.ambient)
                                    }
                                    
                                    try audioSession.setActive(true)
                                } catch {
                                    isActivated = false
                                }
                                
                                if isActivated {
                                    await MainActor.run {
                                        guard let self else {
                                            return
                                        }
                                        
                                        self.audioPlayer = audioPlayer
                                        self.audioPlayer!.delegate = self
                                        self.audioPlayer!.volume = self.isMute == true ? 0.0 : 1.0
                                        self.audioPlayer!.play()
                                    }
                                }
                            }
                        }
                    }
                }
            } else if prior > stars {
                let characters = Script.shared.characters
                let priorRuntime = Script.Runtime()
                let currentRuntime = Script.Runtime()
                var nameSet = Set<String>()
                
                for characterView in self.characterViews {
                    guard let name = characterView.name, nameSet.insert(name).inserted else {
                        continue
                    }
                    
                    let starSequences = characters.reduce(into: [Sequence]()) { result, character in
                        if character.name == name {
                            result.append(contentsOf: character.sequences.filter { $0.name == "Star" })
                        }
                    }
                    let startSequences = characters.reduce(into: [Sequence]()) { result, character in
                        if character.name == name {
                            result.append(contentsOf: character.sequences.filter { $0.name == "Start" })
                        }
                    }
                    
                    await priorRuntime.run(characters: characters, name: name, sequences: starSequences, state: String(prior), scores: [:], words: []) { _ in
                        []
                    }
                    await priorRuntime.run(characters: characters, name: name, sequences: startSequences, scores: [:], words: []) { _ in
                        []
                    }
                    await currentRuntime.run(characters: characters, name: name, sequences: starSequences, state: state, scores: [:], words: []) { _ in
                        []
                    }
                    await currentRuntime.run(characters: characters, name: name, sequences: startSequences, scores: [:], words: []) { _ in
                        []
                    }
                }
                
                let priorStates = priorRuntime.states
                let currentStates = currentRuntime.states
                let keys = Set(priorStates.keys).union(currentStates.keys)
                
                if !(await Script.shared.update { states in
                    guard self.stars == stars else {
                        return false
                    }
                    
                    for key in keys where priorStates[key] != currentStates[key] && states[key] == priorStates[key] {
                        states[key] = currentStates[key]
                    }
                    
                    return true
                }) {
                    return
                }
            }
            
            if let userDefaults = UserDefaults(suiteName: "group.com.milchchan.Apricot") {
                userDefaults.setValue(stars, forKey: "stars")
                
                WidgetCenter.shared.reloadAllTimelines()
            }
            
            self.delegate?.agentDidUpdate(self, background: background)
        }
    }
    
    override func didMoveToWindow() {
        super.didMoveToWindow()
        
        if let window = self.window {
            let systemScale = self.systemScale
            let safeBounds = window.bounds.inset(by: window.safeAreaInsets)
            
            if safeBounds.width > safeBounds.height {
                let interval = safeBounds.width / Double(max(self.characterViews.count, 1))
                var maxWidth = 0.0
                var maxHeight = 0.0
                
                for i in 0..<self.characterViews.count {
                    let characterView = self.characterViews[i]
                    let preferredScale = (characterView.scale == 0.0 ? self.traitCollection.displayScale : characterView.scale) * self.userScale / self.traitCollection.displayScale
                    
                    characterView.alpha = 1.0
                    characterView.transform.tx = safeBounds.maxX - interval * (Double(i) + 0.5) - window.bounds.midX
                    maxWidth = max(max(characterView.size.width - characterView.contentInsets.leading - characterView.contentInsets.trailing, 0.0) * preferredScale, maxWidth)
                    maxHeight = max(max(abs(characterView.size.height) - characterView.contentInsets.top, 0.0) * preferredScale, maxHeight)
                }
                
                self.systemScale = min(maxWidth > 0.0 ? interval / maxWidth : 1.0, maxHeight > 0.0 ? safeBounds.height / 2.0 / maxHeight : 1.0, 1.0)
            } else {
                let tx = safeBounds.midX - window.bounds.midX
                
                for i in 0..<self.characterViews.count {
                    let characterView = self.characterViews[i]
                    
                    if i > 0 {
                        characterView.alpha = 0.0
                    }
                    
                    characterView.transform.tx = tx
                }
                
                self.systemScale = 1.0
            }
            
            if self.systemScale == systemScale {
                for characterView in self.characterViews {
                    let images: [String: CGImage]
                    let imageScale: Double
                    
                    if characterView.imageScale > 1.0 && characterView.cachedImages.values.allSatisfy({ $0.1 != nil }) {
                        images = characterView.cachedImages.mapValues { $0.1! }
                        imageScale = characterView.imageScale
                    } else {
                        images = characterView.cachedImages.mapValues { $0.0 }
                        imageScale = 1.0
                    }
                    
                    let (image, fades) = characterView.preview(timelines: characterView.cachedTimelines, images: images, imageScale: imageScale)
                    
                    if let image {
                        let actualScale = self.userScale * self.systemScale
                        let imageScale = (characterView.scale == 0.0 ? 1.0 : characterView.scale / self.traitCollection.displayScale) * actualScale
                        let imageSize = CGSize(width: ceil(characterView.size.width * imageScale), height: ceil(characterView.size.height * imageScale))
                        let format = UIGraphicsImageRendererFormat(for: self.traitCollection)
                        
                        format.opaque = false
                        format.preferredRange = .standard
                        format.scale = self.traitCollection.displayScale
                        
                        let renderer = UIGraphicsImageRenderer(size: imageSize, format: format)
                        let renderedImage = renderer.image { rendererContext in
                            let context = rendererContext.cgContext
                            
                            if actualScale == floor(actualScale) {
                                context.interpolationQuality = .none
                                context.setAllowsAntialiasing(false)
                            } else {
                                context.interpolationQuality = .high
                                context.setAllowsAntialiasing(true)
                            }
                            
                            context.clear(CGRect(origin: CGPoint.zero, size: imageSize))
                            
                            if characterView.isMirror {
                                context.translateBy(x: imageSize.width, y: imageSize.height)
                                context.scaleBy(x: -1.0, y: -1.0)
                            } else {
                                context.translateBy(x: 0, y: imageSize.height)
                                context.scaleBy(x: 1.0, y: -1.0)
                            }
                            
                            context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: imageSize.width, height: imageSize.height))
                        }
                        
                        if let image = renderedImage.cgImage {
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            characterView.contentView.layer.contents = image
                            
                            CATransaction.commit()
                        }
                        
                        for (key, value) in fades {
                            characterView.fades[key] = value
                        }
                    }
                    
                    characterView.cachedTimelines.removeAll()
                }
            } else {
                for characterView in self.characterViews {
                    let preferredScale = (characterView.scale == 0.0 ? self.traitCollection.displayScale : characterView.scale) * self.userScale * self.systemScale
                    let frame = CGRect(x: characterView.origin.x * preferredScale / self.traitCollection.displayScale, y: characterView.origin.y * preferredScale / self.traitCollection.displayScale, width: characterView.size.width * preferredScale / self.traitCollection.displayScale, height: characterView.size.height * preferredScale / self.traitCollection.displayScale)
                    let messageWidth = characterView.constraints.reduce(0.0, { $1.firstItem === characterView.balloonView && $1.firstAttribute == .width ? $1.constant : $0 })
                    let maxScale = messageWidth > 0.0 ? (messageWidth + 16.0) / messageWidth : 0.0
                    let balloonHeight = characterView.constraints.reduce(0.0, { $1.firstItem === characterView.balloonView && $1.firstAttribute == .height ? $1.constant : $0 })
                    let horizontalPadding = round((characterView.contentInsets.leading + characterView.contentInsets.trailing) * preferredScale / self.traitCollection.displayScale / 2.0)
                    let verticalPadding = round((characterView.contentInsets.top + characterView.contentInsets.bottom) * preferredScale / self.traitCollection.displayScale / 2.0)
                    
                    for motionEffect in characterView.contentView.motionEffects {
                        if let motionEffectGroup = motionEffect as? UIMotionEffectGroup, let motionEffects = motionEffectGroup.motionEffects {
                            for me in motionEffects {
                                if let interpolatingMotionEffect = me as? UIInterpolatingMotionEffect {
                                    if interpolatingMotionEffect.type == .tiltAlongHorizontalAxis {
                                        interpolatingMotionEffect.minimumRelativeValue = -horizontalPadding
                                        interpolatingMotionEffect.maximumRelativeValue = horizontalPadding
                                    } else if interpolatingMotionEffect.type == .tiltAlongVerticalAxis {
                                        interpolatingMotionEffect.minimumRelativeValue = -verticalPadding
                                        interpolatingMotionEffect.maximumRelativeValue = verticalPadding
                                    }
                                }
                            }
                        }
                    }
                    
                    for constraint in characterView.constraints {
                        if constraint.firstItem === characterView {
                            if constraint.firstAttribute == .width {
                                constraint.constant = ceil(max(frame.width, messageWidth * maxScale))
                            } else if constraint.firstAttribute == .height {
                                constraint.constant = ceil(frame.height + balloonHeight * maxScale - frame.origin.y)
                            }
                        } else if constraint.firstItem === characterView.contentView && constraint.secondItem === characterView {
                            if constraint.firstAttribute == .width {
                                constraint.constant = -floor(max(frame.width, messageWidth * maxScale) - frame.width)
                            } else if constraint.firstAttribute == .height {
                                constraint.constant = -floor(balloonHeight * maxScale - frame.origin.y)
                            }
                        } else if constraint.firstItem === characterView.balloonView {
                            if constraint.firstAttribute == .width {
                                constraint.constant = messageWidth
                            } else if constraint.firstAttribute == .bottom {
                                constraint.constant = round(balloonHeight / 2.0 - frame.origin.y)
                            }
                        }
                    }
                    
                    let images: [String: CGImage]
                    let imageScale: Double
                    
                    if characterView.imageScale > 1.0 && characterView.cachedImages.values.allSatisfy({ $0.1 != nil }) {
                        images = characterView.cachedImages.mapValues { $0.1! }
                        imageScale = characterView.imageScale
                    } else {
                        images = characterView.cachedImages.mapValues { $0.0 }
                        imageScale = 1.0
                    }
                    
                    let (image, fades) = characterView.preview(timelines: characterView.cachedTimelines, images: images, imageScale: imageScale)
                    
                    if let image {
                        let actualScale = self.userScale * self.systemScale
                        let imageScale = (characterView.scale == 0.0 ? 1.0 : characterView.scale / self.traitCollection.displayScale) * actualScale
                        let imageSize = CGSize(width: ceil(characterView.size.width * imageScale), height: ceil(characterView.size.height * imageScale))
                        let format = UIGraphicsImageRendererFormat(for: self.traitCollection)
                        
                        format.opaque = false
                        format.preferredRange = .standard
                        format.scale = self.traitCollection.displayScale
                        
                        let renderer = UIGraphicsImageRenderer(size: imageSize, format: format)
                        let renderedImage = renderer.image { rendererContext in
                            let context = rendererContext.cgContext
                            
                            if actualScale == floor(actualScale) {
                                context.interpolationQuality = .none
                                context.setAllowsAntialiasing(false)
                            } else {
                                context.interpolationQuality = .high
                                context.setAllowsAntialiasing(true)
                            }
                            
                            context.clear(CGRect(origin: CGPoint.zero, size: imageSize))
                            
                            if characterView.isMirror {
                                context.translateBy(x: imageSize.width, y: imageSize.height)
                                context.scaleBy(x: -1.0, y: -1.0)
                            } else {
                                context.translateBy(x: 0, y: imageSize.height)
                                context.scaleBy(x: 1.0, y: -1.0)
                            }
                            
                            context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: imageSize.width, height: imageSize.height))
                        }
                        
                        if let image = renderedImage.cgImage {
                            CATransaction.begin()
                            CATransaction.setDisableActions(true)
                            
                            characterView.contentView.layer.contents = image
                            
                            CATransaction.commit()
                        }
                        
                        for (key, value) in fades {
                            characterView.fades[key] = value
                        }
                    }
                    
                    characterView.cachedTimelines.removeAll()
                }
            }
            
            if self.displayLink == nil {
                let displayLink = CADisplayLink(target: self, selector: #selector(self.step))
                
                self.displayLink = displayLink
                displayLink.add(to: .current, forMode: .common)
            }
            
            Task { [weak self] in
                await AgentView.Upscaler.shared.run(characters: Script.shared.characters) { @MainActor [weak self] total in
                    guard total > 0 else {
                        return
                    }

                    Task.detached {
                        let image = UIImage(systemName: "sparkles", withConfiguration: UIImage.SymbolConfiguration(font: .systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .caption1).pointSize, weight: .bold)))!

                        await MainActor.run {
                            if let self, let characterView = self.characterViews.first {
                                self.notify(characterView: characterView, image: image, text: nil, duration: 5.0)
                            }
                        }
                    }
                }
            }
        } else {
            self.displayLink?.invalidate()
            self.displayLink = nil
            self.frameTimestamps.removeAll(keepingCapacity: true)
        }
    }
    
    override func layoutSubviews() {
        super.layoutSubviews()
        
        let systemScale = self.systemScale
        let safeBounds = self.bounds.inset(by: self.safeAreaInsets)
        
        if let window = self.window, let windowScene = window.windowScene, windowScene.effectiveGeometry.isInteractivelyResizing {
            if safeBounds.width > safeBounds.height {
                let interval = safeBounds.width / Double(max(self.characterViews.count, 1))
                var maxWidth = 0.0
                var maxHeight = 0.0
                
                for i in 0..<self.characterViews.count {
                    let characterView = self.characterViews[i]
                    let preferredScale = (characterView.scale == 0.0 ? self.traitCollection.displayScale : characterView.scale) * self.userScale / self.traitCollection.displayScale
                    
                    if characterView.alpha != 1.0 {
                        UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseOut, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
                            characterView.alpha = 1.0
                        })
                    }
                    
                    characterView.transform.tx = safeBounds.maxX - interval * (Double(i) + 0.5) - self.bounds.midX
                    maxWidth = max(max(characterView.size.width - characterView.contentInsets.leading - characterView.contentInsets.trailing, 0.0) * preferredScale, maxWidth)
                    maxHeight = max(max(abs(characterView.size.height) - characterView.contentInsets.top, 0.0) * preferredScale, maxHeight)
                }
                
                self.systemScale = min(maxWidth > 0.0 ? interval / maxWidth : 1.0, maxHeight > 0.0 ? safeBounds.height / 2.0 / maxHeight : 1.0, 1.0)
            } else {
                let tx = safeBounds.midX - self.bounds.midX
                
                for i in 0..<self.characterViews.count {
                    let characterView = self.characterViews[i]
                    
                    if i > 0 && characterView.alpha != 0.0 {
                        UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseIn, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
                            characterView.alpha = 0.0
                        })
                    }
                    
                    characterView.transform.tx = tx
                }
                
                self.systemScale = 1.0
            }
            
            if self.systemScale != systemScale {
                for characterView in self.characterViews {
                    let preferredScale = (characterView.scale == 0.0 ? self.traitCollection.displayScale : characterView.scale) * self.userScale * self.systemScale
                    let frame = CGRect(x: characterView.origin.x * preferredScale / self.traitCollection.displayScale, y: characterView.origin.y * preferredScale / self.traitCollection.displayScale, width: characterView.size.width * preferredScale / self.traitCollection.displayScale, height: characterView.size.height * preferredScale / self.traitCollection.displayScale)
                    let messageWidth = characterView.constraints.reduce(0.0, { $1.firstItem === characterView.balloonView && $1.firstAttribute == .width ? $1.constant : $0 })
                    let maxScale = messageWidth > 0.0 ? (messageWidth + 16.0) / messageWidth : 0.0
                    let balloonHeight = characterView.constraints.reduce(0.0, { $1.firstItem === characterView.balloonView && $1.firstAttribute == .height ? $1.constant : $0 })
                    let horizontalPadding = round((characterView.contentInsets.leading + characterView.contentInsets.trailing) * preferredScale / self.traitCollection.displayScale / 2.0)
                    let verticalPadding = round((characterView.contentInsets.top + characterView.contentInsets.bottom) * preferredScale / self.traitCollection.displayScale / 2.0)
                    
                    for motionEffect in characterView.contentView.motionEffects {
                        if let motionEffectGroup = motionEffect as? UIMotionEffectGroup, let motionEffects = motionEffectGroup.motionEffects {
                            for me in motionEffects {
                                if let interpolatingMotionEffect = me as? UIInterpolatingMotionEffect {
                                    if interpolatingMotionEffect.type == .tiltAlongHorizontalAxis {
                                        interpolatingMotionEffect.minimumRelativeValue = -horizontalPadding
                                        interpolatingMotionEffect.maximumRelativeValue = horizontalPadding
                                    } else if interpolatingMotionEffect.type == .tiltAlongVerticalAxis {
                                        interpolatingMotionEffect.minimumRelativeValue = -verticalPadding
                                        interpolatingMotionEffect.maximumRelativeValue = verticalPadding
                                    }
                                }
                            }
                        }
                    }
                    
                    for constraint in characterView.constraints {
                        if constraint.firstItem === characterView {
                            if constraint.firstAttribute == .width {
                                constraint.constant = ceil(max(frame.width, messageWidth * maxScale))
                            } else if constraint.firstAttribute == .height {
                                constraint.constant = ceil(frame.height + balloonHeight * maxScale - frame.origin.y)
                            }
                        } else if constraint.firstItem === characterView.contentView && constraint.secondItem === characterView {
                            if constraint.firstAttribute == .width {
                                constraint.constant = -floor(max(frame.width, messageWidth * maxScale) - frame.width)
                            } else if constraint.firstAttribute == .height {
                                constraint.constant = -floor(balloonHeight * maxScale - frame.origin.y)
                            }
                        } else if constraint.firstItem === characterView.balloonView {
                            if constraint.firstAttribute == .width {
                                constraint.constant = messageWidth
                            } else if constraint.firstAttribute == .bottom {
                                constraint.constant = round(balloonHeight / 2.0 - frame.origin.y)
                            }
                        }
                    }
                    
                    if !characterView.cachedTimelines.isEmpty {
                        let images: [String: CGImage]
                        let imageScale: Double
                        
                        if characterView.imageScale > 1.0 && characterView.cachedImages.values.allSatisfy({ $0.1 != nil }) {
                            images = characterView.cachedImages.mapValues { $0.1! }
                            imageScale = characterView.imageScale
                        } else {
                            images = characterView.cachedImages.mapValues { $0.0 }
                            imageScale = 1.0
                        }
                        
                        let (image, _) = characterView.preview(timelines: characterView.cachedTimelines, images: images, imageScale: imageScale)
                        
                        if let image {
                            let actualScale = self.userScale * self.systemScale
                            let imageScale = (characterView.scale == 0.0 ? 1.0 : characterView.scale / self.traitCollection.displayScale) * actualScale
                            let imageSize = CGSize(width: ceil(characterView.size.width * imageScale), height: ceil(characterView.size.height * imageScale))
                            let format = UIGraphicsImageRendererFormat(for: self.traitCollection)
                            
                            format.opaque = false
                            format.preferredRange = .standard
                            format.scale = self.traitCollection.displayScale
                            
                            let renderer = UIGraphicsImageRenderer(size: imageSize, format: format)
                            let renderedImage = renderer.image { rendererContext in
                                let context = rendererContext.cgContext
                                
                                if actualScale == floor(actualScale) {
                                    context.interpolationQuality = .none
                                    context.setAllowsAntialiasing(false)
                                } else {
                                    context.interpolationQuality = .high
                                    context.setAllowsAntialiasing(true)
                                }
                                
                                context.clear(CGRect(origin: CGPoint.zero, size: imageSize))
                                
                                if characterView.isMirror {
                                    context.translateBy(x: imageSize.width, y: imageSize.height)
                                    context.scaleBy(x: -1.0, y: -1.0)
                                } else {
                                    context.translateBy(x: 0, y: imageSize.height)
                                    context.scaleBy(x: 1.0, y: -1.0)
                                }
                                
                                context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: imageSize.width, height: imageSize.height))
                            }
                            
                            if let image = renderedImage.cgImage {
                                CATransaction.begin()
                                CATransaction.setDisableActions(true)
                                
                                characterView.contentView.layer.contents = image
                                
                                CATransaction.commit()
                            }
                        }
                    }
                }
            }
        } else {
            if safeBounds.width > safeBounds.height {
                let interval = safeBounds.width / Double(max(self.characterViews.count, 1))
                var maxWidth = 0.0
                var maxHeight = 0.0
                
                for i in 0..<self.characterViews.count {
                    let characterView = self.characterViews[i]
                    let preferredScale = (characterView.scale == 0.0 ? self.traitCollection.displayScale : characterView.scale) * self.userScale / self.traitCollection.displayScale
                    let tx = safeBounds.maxX - interval * (Double(i) + 0.5) - self.bounds.midX
                    
                    if characterView.transform.tx != tx || (i > 0 && characterView.alpha != 1.0) {
                        if i > 0 {
                            UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseOut, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
                                characterView.alpha = 1.0
                                characterView.transform.tx = tx
                            })
                        } else {
                            UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseOut, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
                                characterView.transform.tx = tx
                            })
                        }
                    }
                    
                    maxWidth = max(max(characterView.size.width - characterView.contentInsets.leading - characterView.contentInsets.trailing, 0.0) * preferredScale, maxWidth)
                    maxHeight = max(max(abs(characterView.size.height) - characterView.contentInsets.top, 0.0) * preferredScale, maxHeight)
                }
                
                self.systemScale = min(maxWidth > 0.0 ? interval / maxWidth : 1.0, maxHeight > 0.0 ? safeBounds.height / 2.0 / maxHeight : 1.0, 1.0)
            } else {
                let tx = safeBounds.midX - self.bounds.midX
                
                for i in 0..<self.characterViews.count {
                    let characterView = self.characterViews[i]
                    
                    if characterView.transform.tx != tx || (i > 0 && characterView.alpha != 0.0) {
                        if i > 0 {
                            UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseIn, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
                                characterView.alpha = 0.0
                                characterView.transform.tx = tx
                            })
                        } else {
                            UIView.animate(withDuration: 0.5, delay: 0.0, options: [.curveEaseIn, .allowUserInteraction, .beginFromCurrentState, .overrideInheritedDuration, .overrideInheritedCurve], animations: {
                                characterView.transform.tx = tx
                            })
                        }
                    }
                }
                
                self.systemScale = 1.0
            }
            
            if self.systemScale != systemScale {
                self.change(scale: self.userScale)
            }
        }
    }
    
    override func hitTest(_ point: CGPoint, with event: UIEvent?) -> UIView? {
        let view = super.hitTest(point, with: event)
        
        if let button = view as? UIButton {
            return button
        }
        
        for characterView in self.characterViews {
            if view === characterView.contentView {
                let format = UIGraphicsImageRendererFormat(for: self.traitCollection)
                
                format.opaque = false
                format.preferredRange = .standard
                format.scale = self.traitCollection.displayScale
                
                let renderer = UIGraphicsImageRenderer(size: view!.frame.size, format: format)
                let image = renderer.image { _ in
                    view!.drawHierarchy(in: view!.bounds, afterScreenUpdates: false)
                }.cgImage
                
                if let image, self.getColor(image: image, scale: self.traitCollection.displayScale, x: Int(floor(point.x - characterView.frame.origin.x - characterView.contentView.superview!.frame.origin.x)), y: Int(floor(point.y - characterView.frame.origin.y - characterView.frame.size.height + characterView.contentView.frame.size.height))).alpha > 0.0 {
                    return view
                }
            } else if let messageLabel = view as? UILabel {
                return view!.hitTest(CGPoint(x: point.x - characterView.frame.origin.x - characterView.balloonView!.frame.origin.x - messageLabel.frame.origin.x, y: point.y - characterView.frame.origin.y - (characterView.frame.height - characterView.balloonView!.frame.origin.y - characterView.balloonView!.frame.height) - messageLabel.frame.origin.y), with: event)
            } else {
                for subview in characterView.balloonView!.subviews {
                    if let visualEffectView = subview as? UIVisualEffectView, let maskView = visualEffectView.mask, let sublayers = maskView.layer.sublayers {
                        for sublayer in sublayers {
                            if let maskLayer = sublayer as? CAShapeLayer, let path = maskLayer.path {
                                var transform = CGAffineTransformMakeScale(characterView.balloonView!.transform.a, -characterView.balloonView!.transform.d)
                                
                                if let p = path.copy(using: &transform), p.contains(CGPoint(x: point.x - characterView.frame.origin.x - characterView.balloonView!.frame.origin.x, y: point.y - characterView.frame.origin.y - (characterView.frame.height - characterView.balloonView!.frame.origin.y - characterView.balloonView!.frame.height))) {
                                    return view
                                }
                            }
                        }
                    }
                }
            }
        }
        
        return nil
    }
    
    @objc private func doubleTapped(sender: UITapGestureRecognizer) {
        if sender.state == .ended {
            for characterView in self.characterViews {
                if characterView.contentView === sender.view {
                    let types = characterView.types.compactMap({ $0.value.1 ? $0.key : nil })
                    var state: String? = nil
                    
                    for timeline in characterView.cachedTimelines {
                        if !timeline.animation.isEmpty {
                            let current = timeline.current
                            
                            if current.opacity > 0.0, let path = current.path {
                                let key: String
                                var tempTypes: [String?]? = nil
                                var isVisible: Bool
                                let isTouchable: Bool
                                
                                if timeline.animation.type == nil {
                                    key = String(timeline.animation.z)
                                    tempTypes = []
                                    
                                    for cachedTimeline in characterView.cachedTimelines {
                                        if cachedTimeline.animation.z == timeline.animation.z {
                                            tempTypes!.append(cachedTimeline.animation.type)
                                        }
                                    }
                                } else {
                                    key = "\(timeline.animation.z)&\(timeline.animation.type!)"
                                }
                                
                                let fade = characterView.fades[key]
                                
                                if let tempTypes {
                                    if types.isEmpty {
                                        isVisible = true
                                    } else {
                                        isVisible = !tempTypes.contains { type in
                                            if let type {
                                                return types.contains(type)
                                            }
                                            
                                            return false
                                        }
                                    }
                                } else if types.isEmpty {
                                    isVisible = false
                                } else {
                                    tempTypes = []
                                    
                                    for cachedTimeline in characterView.cachedTimelines {
                                        if let type = cachedTimeline.animation.type, cachedTimeline.animation.z == timeline.animation.z && types.contains(type) {
                                            tempTypes!.append(type)
                                        }
                                    }
                                    
                                    isVisible = !tempTypes!.isEmpty && tempTypes!.lastIndex(of: timeline.animation.type!) == tempTypes!.count - 1
                                }
                                
                                if isVisible {
                                    if let fade {
                                        isTouchable = fade > 0.0
                                    } else {
                                        isTouchable = timeline.animation.type == nil
                                    }
                                } else if let fade {
                                    isTouchable = fade > 0.0
                                } else {
                                    isTouchable = false
                                }
                                
                                if isTouchable, let image = characterView.cachedImages[path]?.0 {
                                    let location = sender.location(in: sender.view)
                                    let x = round(characterView.origin.x + current.location.x)
                                    let y = round(characterView.origin.y + current.location.y)
                                    var width = current.size.width
                                    var height = current.size.height
                                    let scale = (characterView.scale == 0.0 ? 1.0 : characterView.scale / self.traitCollection.displayScale) * self.userScale * self.systemScale
                                    
                                    if width == 0.0 && height == 0.0 {
                                        width = Double(image.width)
                                        height = Double(image.height)
                                    } else if width == 0.0 {
                                        width = height * Double(image.width) / Double(image.height)
                                    } else if height == 0.0 {
                                        height = width * Double(image.height) / Double(image.width)
                                    }
                                    
                                    if x * scale <= location.x && location.x < (x + floor(width)) * scale && y * scale <= location.y && location.y < (y + floor(height)) * scale {
                                        state = current.path;
                                    }
                                }
                            }
                        }
                    }
                    
                    Task {
                        await Script.shared.run(name: characterView.name!, sequences: Script.shared.characters.reduce(into: [], { x, y in
                            if y.name == characterView.name {
                                for sequence in y.sequences {
                                    if sequence.name == "DoubleClick" || sequence.name == "DoubleTap" {
                                        x.append(sequence)
                                    }
                                }
                            }
                        }), state: state, words: []) { sequences in
                            Script.shared.queue.insert(contentsOf: sequences.reduce(into: [], { x, value in
                                var y = value
                                y.append(.completion)
                                x.append((characterView.name!, y))
                            }), at: 0)
                            
                            return []
                        }
                    }
                    
                    break
                }
            }
        }
    }
    
    @objc private func step(displayLink: CADisplayLink) {
        if self.frame.size.width > 0 && self.frame.size.height > 0 && self.isRunning {
            let frameRateSampleDuration = 1.0
            let frameRateDropThreshold = 0.25
            let deltaTime = self.frameTimestamps.last.map { displayLink.timestamp - $0 } ?? (displayLink.targetTimestamp - displayLink.timestamp)
            let averageFrameInterval = self.frameTimestamps.count > 1 ? (self.frameTimestamps.last! - self.frameTimestamps[0]) / Double(self.frameTimestamps.count - 1) : deltaTime
            let isFrameRateDropping = self.frameTimestamps.count > 1 && deltaTime + 0.000001 >= averageFrameInterval / (1.0 - frameRateDropThreshold)
            
            self.frameTimestamps.append(displayLink.timestamp)
            self.frameTimestamps.removeAll { $0 < displayLink.timestamp - frameRateSampleDuration }
            
            for characterView in self.characterViews {
                if characterView.isLoaded {
                    var isReady = true
                    
                    if characterView.stagingTimelines.isEmpty {
                        isReady = self.dispatch(characterView: characterView)
                    }
                    
                    if isReady {
                        var redrawRequired = false
                        
                        if characterView.elapsedTime < characterView.maxDuration {
                            let isVisible = (characterView.parentView.map({
                                let safeBounds = $0.bounds.inset(by: $0.safeAreaInsets)
                                
                                return safeBounds.width > safeBounds.height
                            }) ?? false) || self.characterViews.firstIndex(of: characterView) == 0
                            
                            if isVisible {
                                if characterView.elapsedTime > 0 {
                                    var indexSet = Set<Int>()
                                    var index = 0
                                    
                                    if !characterView.nextTimelines.isEmpty {
                                        var isEnded = true
                                        
                                        for timeline in characterView.cachedTimelines {
                                            if timeline.animation.repeats > 0 && timeline.time < timeline.duration {
                                                isEnded = false
                                                
                                                break
                                            }
                                        }
                                        
                                        if isEnded {
                                            for (timeline, nextTimeline) in characterView.nextTimelines {
                                                if let i = characterView.cachedTimelines.firstIndex(where: { $0 === timeline }) {
                                                    nextTimeline.time = 0.0
                                                    characterView.cachedTimelines[i] = nextTimeline
                                                    indexSet.insert(i)
                                                    redrawRequired = true
                                                }
                                            }
                                            
                                            characterView.nextTimelines.removeAll()
                                        }
                                    }
                                    
                                    for timeline in characterView.cachedTimelines {
                                        if !indexSet.contains(index) {
                                            let previous = timeline.current
                                            
                                            timeline.time += deltaTime
                                            
                                            if timeline.animation.repeats == 0 && timeline.time > timeline.duration && characterView.nextTimelines.isEmpty {
                                                timeline.time = timeline.time.truncatingRemainder(dividingBy: timeline.duration)
                                            }
                                            
                                            if previous != timeline.current {
                                                redrawRequired = true
                                            }
                                        }
                                        
                                        index += 1
                                    }
                                } else {
                                    redrawRequired = true
                                }
                                
                                characterView.elapsedTime += deltaTime
                            } else {
                                let animationDeltaTime = characterView.maxDuration - characterView.elapsedTime
                                var indexSet = Set<Int>()
                                
                                for (timeline, nextTimeline) in characterView.nextTimelines {
                                    if let i = characterView.cachedTimelines.firstIndex(where: { $0 === timeline }) {
                                        nextTimeline.time = max(0.0, animationDeltaTime - max(0.0, timeline.duration - timeline.time))
                                        
                                        if nextTimeline.animation.repeats == 0 && nextTimeline.time > nextTimeline.duration {
                                            nextTimeline.time = nextTimeline.time.truncatingRemainder(dividingBy: nextTimeline.duration)
                                        }
                                        
                                        characterView.cachedTimelines[i] = nextTimeline
                                        indexSet.insert(i)
                                    }
                                }
                                
                                characterView.nextTimelines.removeAll()
                                
                                for (index, timeline) in characterView.cachedTimelines.enumerated() {
                                    if !indexSet.contains(index) {
                                        timeline.time += animationDeltaTime
                                        
                                        if timeline.animation.repeats == 0 && timeline.time > timeline.duration {
                                            timeline.time = timeline.time.truncatingRemainder(dividingBy: timeline.duration)
                                        }
                                    }
                                }
                                
                                characterView.elapsedTime = characterView.maxDuration
                                redrawRequired = true
                            }
                            
                            if redrawRequired {
                                let sprites = characterView.cachedTimelines.map { $0.current }
                                
                                redrawRequired = sprites != characterView.sprites
                                
                                if redrawRequired {
                                    characterView.sprites = sprites
                                }
                            }
                        }
                        
                        if characterView.isInvalidated || redrawRequired {
                            let isStaging = !characterView.stagingTimelines.isEmpty
                            let images: [String: CGImage]
                            let imageScale: Double
                            
                            if !isFrameRateDropping && characterView.imageScale > 1.0 && characterView.cachedImages.values.allSatisfy({ $0.1 != nil }) {
                                images = characterView.cachedImages.mapValues { $0.1! }
                                imageScale = characterView.imageScale
                            } else {
                                images = characterView.cachedImages.mapValues { $0.0 }
                                imageScale = 1.0
                            }
                            
                            let (image, completed) = characterView.render(timelines: characterView.cachedTimelines, images: images, imageScale: imageScale, deltaTime: deltaTime)
                            
                            characterView.isInvalidated = !completed
                            
                            if self.characterViews.firstIndex(of: characterView) == 0, let image {
                                if !isStaging {
                                    self.snapshot = (self.snapshot.0, image)
                                }
                                
                                self.delegate?.agentDidRender(self, image: image, by: characterView.name!)
                            }
                            
                            if completed && isStaging {
                                characterView.isInvalidated = true
                                characterView.elapsedTime = 0.0
                                characterView.cachedTimelines.removeAll()
                                characterView.cachedTimelines.append(contentsOf: characterView.stagingTimelines)
                                characterView.stagingTimelines.removeAll()
                            }
                        }
                    }
                }
                
                if !characterView.messageQueue.isEmpty {
                    if var step = characterView.messageQueue[0].step {
                        if characterView.messageQueue[0].index == -1 {
                            step -= deltaTime
                            
                            if step > 0.0 {
                                characterView.balloonView!.alpha = sin(step / 2.0 * .pi)
                                characterView.balloonView!.transform = CGAffineTransformMakeScale(sin(step / 2.0 * .pi), -sin(step / 2.0 * .pi))
                                characterView.messageQueue[0].step = step
                            } else {
                                characterView.balloonView!.alpha = 0.0
                                characterView.balloonView!.transform = CGAffineTransformMakeScale(0.0, 0.0)
                                characterView.balloonView!.isHidden = true
                                characterView.messageQueue.removeFirst()
                                
                                for subview in characterView.balloonView!.subviews {
                                    if let visualEffectView = subview as? UIVisualEffectView {
                                        if let view = visualEffectView.contentView.subviews.first(where: { subview in
                                            return subview.subviews.contains(where: { $0 is UILabel })
                                        }) {
                                            for constraint in visualEffectView.contentView.constraints.filter({ constraint in
                                                if constraint.firstItem === view {
                                                    return true
                                                }
                                                
                                                return false
                                            }) {
                                                visualEffectView.contentView.removeConstraint(constraint)
                                            }
                                            
                                            view.removeFromSuperview()
                                        }
                                        
                                        break
                                    }
                                }
                            }
                        } else {
                            step += deltaTime
                            
                            if step < 1.0 {
                                let width = characterView.constraints.reduce(0.0, { $1.firstItem === characterView.balloonView && $1.firstAttribute == .width ? $1.constant : $0 })
                                
                                characterView.balloonView!.alpha = sin(step / 2.0 * .pi)
                                
                                if step > 0.5 {
                                    characterView.balloonView!.transform = CGAffineTransformMakeScale(1.0 + ((width + 16.0) / width - 1.0) * sin(step * .pi), -1.0 - ((width + 16.0) / width - 1.0) * sin(step * .pi))
                                } else {
                                    characterView.balloonView!.transform = CGAffineTransformMakeScale((width + 16.0) / width * sin(step * .pi), -(width + 16.0) / width * sin(step * .pi))
                                }
                                
                                characterView.messageQueue[0].step = step
                            } else {
                                characterView.balloonView!.alpha = 1.0
                                characterView.balloonView!.transform = CGAffineTransformMakeScale(1.0, -1.0)
                                characterView.messageQueue[0].step = nil
                            }
                        }
                    } else if characterView.messageQueue[0].index < characterView.messageQueue[0].lines.count {
                        let index = characterView.messageQueue[0].index
                        let message = characterView.messageQueue[0].lines[index]
                        var updateRequired = false
                        
                        if characterView.messageQueue[0].reverse {
                            if message.type.count > 0 {
                                if characterView.messageQueue[0].slide.step == nil {
                                    var lines = 0
                                    
                                    for i in 0...index {
                                        for j in stride(from: 1, to: characterView.messageQueue[0].lines[i].type.count, by: 1) {
                                            if characterView.messageQueue[0].lines[i].breaks.contains(j) {
                                                lines += 1
                                            }
                                        }
                                        
                                        lines += 1
                                    }
                                    
                                    if lines >= characterView.maxLines && lines - characterView.maxLines == characterView.messageQueue[0].slide.index - 1 && characterView.messageQueue[0].lines[index].breaks.contains(characterView.messageQueue[0].lines[index].type.count) {
                                        characterView.messageQueue[0].slide.index -= 1
                                        characterView.messageQueue[0].slide.step = 1.0
                                    }
                                }
                                
                                if let step = characterView.messageQueue[0].slide.step {
                                    let slideStep = step - deltaTime
                                    
                                    if slideStep <= 0.0 {
                                        characterView.messageQueue[0].slide.step = nil
                                    } else {
                                        characterView.messageQueue[0].slide.step = slideStep
                                    }
                                } else {
                                    characterView.messageQueue[0].lines[index].type.elapsed += deltaTime * characterView.messageQueue[0].speed
                                    
                                    if message.type.elapsed >= 1.0 / message.type.speed {
                                        if message.type.count - 1 < message.text.count {
                                            let width = message.text.count / 2
                                            
                                            if message.type.buffer.count <= width && message.type.count > 0 {
                                                characterView.messageQueue[0].lines[index].type.count -= 1
                                            }
                                            
                                            if !message.type.buffer.isEmpty {
                                                characterView.messageQueue[0].lines[index].type.buffer.remove(at: message.type.buffer.index(message.type.buffer.endIndex, offsetBy: -1))
                                            }
                                        }
                                        
                                        characterView.messageQueue[0].lines[index].type.elapsed = 0.0
                                    }
                                }
                            } else if index > 0 {
                                characterView.messageQueue[0].index -= 1
                                characterView.messageQueue[0].lines[characterView.messageQueue[0].index].type.elapsed = 0.0
                            } else {
                                characterView.messageQueue[0].step = 1.0
                                characterView.messageQueue[0].index = -1
                            }
                        } else if message.type.buffer.count < message.text.count {
                            if characterView.messageQueue[0].slide.step == nil {
                                var lines = 0
                                
                                for i in 0...index {
                                    for j in stride(from: 1, to: characterView.messageQueue[0].lines[i].type.buffer.count, by: 1) {
                                        if characterView.messageQueue[0].lines[i].breaks.contains(j) {
                                            lines += 1
                                        }
                                    }
                                    
                                    lines += 1
                                }
                                
                                if lines >= characterView.maxLines && lines - characterView.maxLines == characterView.messageQueue[0].slide.index && characterView.messageQueue[0].lines[index].breaks.contains(characterView.messageQueue[0].lines[index].type.buffer.count) {
                                    characterView.messageQueue[0].slide.step = 0.0
                                }
                            }
                            
                            if let step = characterView.messageQueue[0].slide.step {
                                let slideStep = step + deltaTime
                                
                                if slideStep >= 1.0 {
                                    characterView.messageQueue[0].slide.index += 1
                                    characterView.messageQueue[0].slide.step = nil
                                } else {
                                    characterView.messageQueue[0].slide.step = slideStep
                                }
                            } else {
                                if message.type.elapsed >= 0.0 {
                                    characterView.messageQueue[0].lines[index].type.elapsed += deltaTime * characterView.messageQueue[0].speed
                                } else {
                                    characterView.messageQueue[0].lines[index].type.elapsed = deltaTime * characterView.messageQueue[0].speed
                                }
                                
                                if message.type.elapsed >= 1.0 / message.type.speed {
                                    if message.type.count >= message.text.count / 2 {
                                        characterView.messageQueue[0].lines[index].type.buffer.append(message.text[message.text.index(message.text.startIndex, offsetBy: message.type.buffer.count)])
                                    }
                                    
                                    if message.type.count < message.text.count {
                                        characterView.messageQueue[0].lines[index].type.count += 1
                                    }
                                    
                                    characterView.messageQueue[0].lines[index].type.elapsed = 0.0
                                }
                            }
                        } else if index < characterView.messageQueue[0].lines.count - 1 {
                            characterView.messageQueue[0].index += 1
                        } else {
                            characterView.messageQueue[0].time += deltaTime
                            
                            if characterView.messageQueue[0].duration >= 0.0 && characterView.messageQueue[0].time >= characterView.messageQueue[0].duration {
                                characterView.messageQueue[0].step = 1.0
                                characterView.messageQueue[0].index = -1
                            }
                        }
                        
                        if message.text.count == characterView.messageQueue[0].lines[index].type.buffer.count {
                            if message.text != characterView.messageQueue[0].lines[index].current {
                                characterView.messageQueue[0].lines[index].current.removeAll()
                                characterView.messageQueue[0].lines[index].current.append(message.text)
                                updateRequired = true
                            }
                        } else {
                            var characters = [Character]()
                            var randomBuffer = String()
                            
                            for k in 0..<message.text.count {
                                let character = message.text[message.text.index(message.text.startIndex, offsetBy: k)]
                                
                                if !character.isNewline && !character.isWhitespace {
                                    characters.append(character)
                                }
                            }
                            
                            if !characters.isEmpty {
                                for k in 0..<characterView.messageQueue[0].lines[index].type.count {
                                    let character = message.text[message.text.index(message.text.startIndex, offsetBy: k)]
                                    
                                    if character.isNewline {
                                        randomBuffer.append(character)
                                    } else {
                                        randomBuffer.append(characters[Int.random(in: 0..<characters.count)])
                                    }
                                }
                            }
                            
                            if randomBuffer.count > characterView.messageQueue[0].lines[index].type.buffer.count {
                                characterView.messageQueue[0].lines[index].current.removeAll()
                                characterView.messageQueue[0].lines[index].current.append(characterView.messageQueue[0].lines[index].type.buffer)
                                characterView.messageQueue[0].lines[index].current.append(String(randomBuffer[randomBuffer.index(randomBuffer.startIndex, offsetBy: characterView.messageQueue[0].lines[index].type.buffer.count)..<randomBuffer.index(randomBuffer.startIndex, offsetBy: randomBuffer.count)]))
                                updateRequired = true
                            } else if characterView.messageQueue[0].lines[index].current.count != characterView.messageQueue[0].lines[index].type.buffer.count {
                                characterView.messageQueue[0].lines[index].current.removeAll()
                                characterView.messageQueue[0].lines[index].current.append(characterView.messageQueue[0].lines[index].type.buffer)
                                updateRequired = true
                            }
                        }
                        
                        if updateRequired {
                            var lines = [[(text: String, highlight: Int?)]]()
                            let accentColor = self.accentColor ?? UIColor(named: "AccentColor")!
                            let language: [(NSAttributedString.Key, Any)] = {
                                if let language = characterView.language {
                                    return [(.languageIdentifier, language)]
                                }
                                
                                return []
                            }()
                            
                            if !characterView.messageQueue[0].lines[index].current.isEmpty {
                                var offset = 0
                                
                                for i in 0..<index {
                                    offset += characterView.messageQueue[0].lines[i].current.count
                                }
                                
                                var components: [(text: String, highlight: Int?)] = [(text: String(characterView.messageQueue[0].lines[index].current[characterView.messageQueue[0].lines[index].current.startIndex]), highlight: characterView.messageQueue[0].attributes.firstIndex(where: { offset >= $0.start && offset < $0.end }))]
                                
                                for i in 1..<characterView.messageQueue[0].lines[index].current.count {
                                    if characterView.messageQueue[0].lines[index].breaks.contains(i) {
                                        lines.append(components)
                                        components = [(text: String(characterView.messageQueue[0].lines[index].current[characterView.messageQueue[0].lines[index].current.index(characterView.messageQueue[0].lines[index].current.startIndex, offsetBy: i)]), highlight: characterView.messageQueue[0].attributes.firstIndex(where: { offset + i >= $0.start && offset + i < $0.end }))]
                                    } else {
                                        let highlight = characterView.messageQueue[0].attributes.firstIndex(where: { offset + i >= $0.start && offset + i < $0.end })
                                        var component = components[components.count - 1]
                                        
                                        if highlight == component.highlight {
                                            component.text.append(characterView.messageQueue[0].lines[index].current[characterView.messageQueue[0].lines[index].current.index(characterView.messageQueue[0].lines[index].current.startIndex, offsetBy: i)])
                                            components[components.count - 1] = component
                                        } else {
                                            components.append((text: String(characterView.messageQueue[0].lines[index].current[characterView.messageQueue[0].lines[index].current.index(characterView.messageQueue[0].lines[index].current.startIndex, offsetBy: i)]), highlight: highlight))
                                        }
                                    }
                                }
                                
                                lines.append(components)
                            }
                            
                            for (index, label) in characterView.messageQueue[0].lines[index].labels.enumerated() {
                                let mutableAttributedString = NSMutableAttributedString()
                                let lineHeight = ceil(label.font!.lineHeight * 1.5)
                                var y = lineHeight * CGFloat(characterView.messageQueue[0].slide.index)
                                
                                if index < lines.count {
                                    let paragraphStyle = NSMutableParagraphStyle()
                                    
                                    paragraphStyle.minimumLineHeight = label.font.lineHeight
                                    paragraphStyle.maximumLineHeight = label.font.lineHeight
                                    
                                    for component in lines[index] {
                                        mutableAttributedString.append(NSAttributedString(string: component.text, attributes: Swift.Dictionary(uniqueKeysWithValues: [(.font, label.font!), (.foregroundColor, component.highlight == nil ? UIColor { $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0) } : accentColor), (.paragraphStyle, paragraphStyle)] + language)))
                                    }
                                }
                                
                                label.attributedText = mutableAttributedString
                                
                                if let step = characterView.messageQueue[0].slide.step {
                                    y += lineHeight * step
                                }
                                
                                label.transform = CGAffineTransformMakeTranslation(0.0, -y)
                            }
                        }
                    }
                }
            }
        } else {
            self.frameTimestamps.removeAll(keepingCapacity: true)
        }
    }
    
    private func make(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, upscaling: Bool, language: String?, sequences: [Sequence], types: [String: (Int, Set<Int>)], insets: (top: Double, left: Double, bottom: Double, right: Double)) -> CharacterView {
        let characterView = CharacterView(frame: .zero)
        let preferredScale = (scale == 0.0 ? self.traitCollection.displayScale : scale) * self.userScale * self.systemScale
        let frame = CGRect(x: location.x * preferredScale / self.traitCollection.displayScale, y: location.y * preferredScale / self.traitCollection.displayScale, width: size.width * preferredScale / self.traitCollection.displayScale, height: insets.bottom * preferredScale / self.traitCollection.displayScale)
        let visualEffectView = UIVisualEffectView(effect: UIGlassEffect(style: .regular))
        let maskView = UIView(frame: .zero)
        let maskLayer = CAShapeLayer()
        let shadowLayer = CAShapeLayer()
        let balloonLayer = CAShapeLayer()
        let doubleTapGestureRecognizer = UITapGestureRecognizer(target: self, action: #selector(self.doubleTapped))
        let horizontalMotionEffect = UIInterpolatingMotionEffect(keyPath: "layer.transform.translation.x", type: .tiltAlongHorizontalAxis)
        let verticalMotionEffect = UIInterpolatingMotionEffect(keyPath: "layer.transform.translation.y", type: .tiltAlongVerticalAxis)
        let motionEffectGroup = UIMotionEffectGroup()
        let contentInsets: NSDirectionalEdgeInsets = .init(top: abs(insets.top), leading: abs(insets.left), bottom: size.height - abs(insets.bottom), trailing: size.width - abs(insets.right))
        let horizontalPadding = round((contentInsets.leading + contentInsets.trailing) * preferredScale / self.traitCollection.displayScale / 2.0)
        let verticalPadding = round((contentInsets.top + contentInsets.bottom) * preferredScale / self.traitCollection.displayScale / 2.0)
        
        doubleTapGestureRecognizer.numberOfTapsRequired = 2
        
        horizontalMotionEffect.minimumRelativeValue = -horizontalPadding
        horizontalMotionEffect.maximumRelativeValue = horizontalPadding
        
        verticalMotionEffect.minimumRelativeValue = -verticalPadding
        verticalMotionEffect.maximumRelativeValue = verticalPadding
        
        motionEffectGroup.motionEffects = [horizontalMotionEffect, verticalMotionEffect]
        
        characterView.parentView = self
        characterView.name = name
        characterView.path = path
        characterView.origin = location
        characterView.size = CGSize(width: size.width, height: insets.bottom)
        characterView.contentInsets = contentInsets
        characterView.scale = scale
        characterView.upscaling = upscaling
        characterView.language = language
        characterView.translatesAutoresizingMaskIntoConstraints = false
        characterView.contentView.addGestureRecognizer(doubleTapGestureRecognizer)
        characterView.contentView.addMotionEffect(motionEffectGroup)
        
        for (key, value) in types {
            characterView.types[key] = (value.0, false, value.1)
        }
        
        self.insertSubview(characterView, at: 0)
        self.addConstraint(NSLayoutConstraint(item: characterView, attribute: .centerX, relatedBy: .equal, toItem: self, attribute: .centerX, multiplier: 1.0, constant: 0.0))
        self.addConstraint(NSLayoutConstraint(item: characterView, attribute: .bottom, relatedBy: .equal, toItem: self.safeAreaLayoutGuide, attribute: .bottom, multiplier: 1.0, constant: -72.0))
        
        characterView.addConstraint(NSLayoutConstraint(item: characterView, attribute: .width, relatedBy: .equal, toItem: nil, attribute: .notAnAttribute, multiplier: 1.0, constant: ceil(frame.width)))
        characterView.addConstraint(NSLayoutConstraint(item: characterView, attribute: .height, relatedBy: .equal, toItem: nil, attribute: .notAnAttribute, multiplier: 1.0, constant: ceil(frame.height - frame.origin.y)))
        
        for constraint in characterView.constraints {
            if constraint.firstItem === characterView.contentView && constraint.secondItem === characterView && constraint.firstAttribute == .height {
                constraint.constant = floor(frame.origin.y)
            }
        }
        
        maskLayer.fillRule = .evenOdd
        maskLayer.strokeColor = UIColor.clear.cgColor
        maskLayer.lineWidth = 0.0
        maskLayer.fillColor = UIColor(white: 1.0, alpha: 1.0).cgColor
        
        shadowLayer.strokeColor = UIColor.clear.cgColor
        shadowLayer.lineWidth = 0.0
        shadowLayer.fillColor = UIColor.clear.cgColor
        shadowLayer.shadowRadius = 8.0
        shadowLayer.shadowOffset = CGSize(width: 0.0, height: 0.0)
        shadowLayer.shadowColor = UIColor(white: 0.0, alpha: 1.0).cgColor
        shadowLayer.shadowOpacity = 0.25
        shadowLayer.mask = maskLayer
        
        balloonLayer.strokeColor = UIColor.clear.cgColor
        balloonLayer.lineWidth = 0.0
        balloonLayer.fillColor = UIColor(white: 1.0, alpha: 1.0).cgColor
        
        characterView.balloonView = UIView()
        characterView.balloonView!.translatesAutoresizingMaskIntoConstraints = false
        characterView.balloonView!.isUserInteractionEnabled = true
        characterView.balloonView!.backgroundColor = .clear
        characterView.balloonView!.alpha = 0.0
        characterView.balloonView!.transform = CGAffineTransformMakeScale(0.0, 0.0)
        characterView.balloonView!.layer.anchorPoint = CGPointMake(0.5, 1.0)
        characterView.balloonView!.layer.addSublayer(shadowLayer)
        characterView.balloonView!.isHidden = true
        
        characterView.insertSubview(characterView.balloonView!, aboveSubview: characterView.contentView)
        
        maskView.layer.addSublayer(balloonLayer)
        
        visualEffectView.translatesAutoresizingMaskIntoConstraints = false
        visualEffectView.isUserInteractionEnabled = true
        visualEffectView.backgroundColor = .clear
        visualEffectView.mask = maskView
        visualEffectView.contentView.isUserInteractionEnabled = true
        
        characterView.balloonView!.addSubview(visualEffectView)
        
        characterView.addConstraint(NSLayoutConstraint(item: characterView.balloonView!, attribute: .centerX, relatedBy: .equal, toItem: characterView, attribute: .centerX, multiplier: 1.0, constant: 0.0))
        characterView.addConstraint(NSLayoutConstraint(item: characterView.balloonView!, attribute: .height, relatedBy: .equal, toItem: nil, attribute: .notAnAttribute, multiplier: 1.0, constant: 0.0))
        characterView.addConstraint(NSLayoutConstraint(item: characterView.balloonView!, attribute: .width, relatedBy: .equal, toItem: nil, attribute: .notAnAttribute, multiplier: 1.0, constant: 0.0))
        characterView.addConstraint(NSLayoutConstraint(item: characterView.balloonView!, attribute: .bottom, relatedBy: .equal, toItem: characterView.contentView, attribute: .bottom, multiplier: 1.0, constant: -round(frame.origin.y)))
        
        characterView.balloonView!.addConstraint(NSLayoutConstraint(item: visualEffectView, attribute: .leading, relatedBy: .equal, toItem: characterView.balloonView!, attribute: .leading, multiplier: 1.0, constant: 0.0))
        characterView.balloonView!.addConstraint(NSLayoutConstraint(item: visualEffectView, attribute: .top, relatedBy: .equal, toItem: characterView.balloonView!, attribute: .top, multiplier: 1.0, constant: 0.0))
        characterView.balloonView!.addConstraint(NSLayoutConstraint(item: visualEffectView, attribute: .trailing, relatedBy: .equal, toItem: characterView.balloonView!, attribute: .trailing, multiplier: 1.0, constant: 0.0))
        characterView.balloonView!.addConstraint(NSLayoutConstraint(item: visualEffectView, attribute: .bottom, relatedBy: .equal, toItem: characterView.balloonView!, attribute: .bottom, multiplier: 1.0, constant: 0.0))
        
        return characterView
    }
    
    private func dispatch(characterView: CharacterView) -> Bool {
        var completed = true
        
        if characterView.elapsedTime >= characterView.maxDuration {
            if characterView.stepQueue.isEmpty {
                if Script.shared.queue.count > 0 {
                    if Script.shared.queue[0].0 == characterView.name {
                        let (name, sequence) = Script.shared.queue.removeFirst()
                        
                        if sequence.name == nil {
                            if sequence.state == nil {
                                self.isRunning = false
                                self.delegate?.agentDidStop(self)
                            } else if !sequence.contains(where: {
                                if case .message = $0 {
                                    return true
                                }
                                
                                return false
                            }) {
                                self.delegate?.agentDidStart(self)
                            }
                            
                            return completed
                        }
                        
                        for step in sequence {
                            characterView.stepQueue.append((name, step))
                        }
                        
                        characterView.lastIdleDate = nil
                        self.delegate?.agentDidTransition(self)
                    } else if !Script.shared.characters.contains(where: { $0.name == Script.shared.queue[0].0 }) {
                        Script.shared.queue.removeFirst()
                    }
                } else if let idleDate = characterView.lastIdleDate {
                    let nowDate = Date()
                    
                    if nowDate.timeIntervalSince(idleDate) >= 10.0 {
                        if let delegate = self.delegate, delegate.agentShouldIdle(self, by: characterView.name!) {
                            Task {
                                await Script.shared.run(name: characterView.name!, sequences: Script.shared.characters.reduce(into: [], { x, y in
                                    if y.name == characterView.name {
                                        for sequence in y.sequences {
                                            if sequence.name == "Idle" {
                                                x.append(sequence)
                                            }
                                        }
                                    }
                                }), words: [])
                            }
                        }
                        
                        characterView.lastIdleDate = nil
                        self.delegate?.agentDidTransition(self)
                    } else if nowDate.timeIntervalSince(characterView.lastTickDate) >= 1.0 {
                        let dateComponents = Calendar.current.dateComponents([.calendar, .timeZone, .era, .year, .month, .day, .hour, .minute], from: nowDate)
                        
                        if dateComponents.minute != Calendar.current.dateComponents([.minute], from: characterView.lastTickDate).minute, let date = dateComponents.date {
                            Task {
                                await Script.shared.run(name: characterView.name!, sequences: Script.shared.characters.reduce(into: [], { x, y in
                                    if y.name == characterView.name {
                                        for sequence in y.sequences {
                                            if sequence.name == "Tick" {
                                                x.append(sequence)
                                            }
                                        }
                                    }
                                }), state: ISO8601DateFormatter.string(from: date, timeZone: .current, formatOptions: [.withFullDate, .withTime, .withDashSeparatorInDate, .withColonSeparatorInTime]), words: []) { x in
                                    var y = x
                                    
                                    y.append(Sequence(name: String()))
                                    
                                    return y
                                }
                            }
                        }
                        
                        characterView.lastTickDate = nowDate
                    }
                } else {
                    characterView.lastIdleDate = Date()
                    self.delegate?.agentDidTransition(self)
                    
                    if self.characterViews.firstIndex(of: characterView) == 0 {
                        var isUpdated = true
                        
                        if characterView.sprites.count == self.snapshot.0.count {
                            isUpdated = false
                            
                            for i in 0..<characterView.sprites.count {
                                if characterView.sprites[i] != self.snapshot.0[i] {
                                    isUpdated = true
                                    
                                    break
                                }
                            }
                        }
                        
                        if isUpdated {
                            self.snapshot = (characterView.sprites, self.snapshot.1)
                            
                            if let image = self.snapshot.1 {
                                Task {
                                    await self.snip(image: image)
                                    
                                    WidgetCenter.shared.reloadAllTimelines()
                                }
                            }
                        }
                    }
                }
            }
            
            if !characterView.stepQueue.isEmpty {
                let safeBounds = self.bounds.inset(by: self.safeAreaInsets)
                
                switch characterView.stepQueue.first!.1 {
                case .message(let message), .synthesis(let message, _):
                    if characterView.balloonView!.isHidden {
                        characterView.stepQueue.removeFirst()
                        
                        if safeBounds.width > safeBounds.height || self.characterViews.firstIndex(of: characterView) == 0 {
                            characterView.show(message: message)
                        }
                    }
                case .animations(let animations):
                    if characterView.timelineQueue.isEmpty {
                        characterView.stepQueue.removeFirst()
                        
                        for animation in animations {
                            characterView.timelineQueue.append(Timeline(animation: animation))
                        }
                    }
                case .sound(let sound):
                    characterView.stepQueue.removeFirst()
                    
                    if safeBounds.width > safeBounds.height || self.characterViews.firstIndex(of: characterView) == 0, let characterPath = characterView.path, let soundPath = sound.path {
                        let path = URL(filePath: characterPath).deletingLastPathComponent().appending(path: soundPath, directoryHint: .inferFromPath).path(percentEncoded: false)
                        
                        Task.detached {
                            if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                defer {
                                    try? file.close()
                                }
                                
                                if let data = try? file.readToEnd(), let audioPlayer = try? AVAudioPlayer(data: data) {
                                    let audioSession = AVAudioSession.sharedInstance()
                                    var isActivated = true
                                    
                                    do {
                                        if audioSession.category != .playAndRecord && audioSession.category != .ambient {
                                            try audioSession.setCategory(.ambient)
                                        }
                                        
                                        try audioSession.setActive(true)
                                    } catch {
                                        isActivated = false
                                    }
                                    
                                    if isActivated {
                                        await MainActor.run {
                                            characterView.audioPlayer = audioPlayer
                                            characterView.audioPlayer!.delegate = characterView
                                            characterView.audioPlayer!.volume = self.isMute ? 0.0 : 1.0
                                            characterView.audioPlayer!.play()
                                        }
                                    }
                                }
                            }
                        }
                    }
                case .audio(let data):
                    characterView.stepQueue.removeFirst()
                    
                    if safeBounds.width > safeBounds.height || self.characterViews.firstIndex(of: characterView) == 0 {
                        Task.detached {
                            if data.count >= 12, let riff = String(data: data[0..<4], encoding: .ascii), riff == "RIFF", let wave = String(data: data[8..<12], encoding: .ascii), wave == "WAVE", let audioPlayer = try? AVAudioPlayer(data: data) {
                                let audioSession = AVAudioSession.sharedInstance()
                                var isActivated = true
                                
                                do {
                                    if audioSession.category != .playAndRecord && audioSession.category != .ambient {
                                        try audioSession.setCategory(.ambient)
                                    }
                                    
                                    try audioSession.setActive(true)
                                } catch {
                                    isActivated = false
                                }
                                
                                if isActivated {
                                    await MainActor.run {
                                        characterView.audioPlayer = audioPlayer
                                        characterView.audioPlayer!.delegate = characterView
                                        characterView.audioPlayer!.volume = self.isMute ? 0.0 : 1.0
                                        characterView.audioPlayer!.play()
                                    }
                                }
                            }
                        }
                    }
                case .sequence, .completion:
                    if characterView.balloonView!.isHidden {
                        if let audioPlayer = characterView.audioPlayer {
                            if !audioPlayer.isPlaying {
                                characterView.stepQueue.removeFirst()
                                characterView.audioPlayer = nil
                            }
                        } else {
                            characterView.stepQueue.removeFirst()
                        }
                    }
                }
            }
            
            if characterView.timelineQueue.isEmpty {
                for timeline in characterView.cachedTimelines {
                    if timeline.animation.repeats == 0 {
                        characterView.timelineQueue.append(timeline)
                    }
                }
            }
            
            if !characterView.timelineQueue.isEmpty {
                var queuedTimelines = characterView.timelineQueue
                var timelines = [Timeline]()
                
                characterView.timelineQueue.removeAll()
                
                repeat {
                    if timelines.contains(where: { $0.animation.z == queuedTimelines.first!.animation.z && $0.animation.type == queuedTimelines.first!.animation.type }) {
                        characterView.timelineQueue.append(queuedTimelines.removeFirst())
                    } else {
                        timelines.append(queuedTimelines.removeFirst())
                    }
                } while !queuedTimelines.isEmpty
                
                if !timelines.isEmpty {
                    var minZIndex = Int.max
                    var maxZIndex = Int.min
                    var zIndexSet = Set<Int>()
                    var imagePaths = [String]()
                    var pathSet = Set<String>()
                    
                    for timeline in timelines {
                        if timeline.animation.z < minZIndex {
                            minZIndex = timeline.animation.z
                        }
                        
                        if timeline.animation.z > maxZIndex {
                            maxZIndex = timeline.animation.z
                        }
                        
                        if !zIndexSet.contains(timeline.animation.z) {
                            zIndexSet.insert(timeline.animation.z)
                        }
                        
                        for sprite in timeline.animation {
                            if let path = sprite.path, !path.isEmpty && !pathSet.contains(path) {
                                if !characterView.cachedImages.keys.contains(path) {
                                    imagePaths.append(path)
                                }
                                
                                pathSet.insert(path)
                            }
                        }
                    }
                    
                    if let characterPath = characterView.path {
                        let pendingTimelines = timelines
                        let pendingMinZIndex = minZIndex
                        let pendingMaxZIndex = maxZIndex
                        let pendingZIndexSet = zIndexSet
                        let pendingPathSet = pathSet
                        
                        characterView.isLoaded = false
                        completed = false
                        
                        Task.immediate { @MainActor [weak self, imagePaths] in
                            guard let self else {
                                return
                            }
                            
                            let paths = Set(imagePaths)
                            let displayScale = Double(self.traitCollection.displayScale)
                            var upscaledPaths = Set<String>()
                            let imageScale = characterView.imageScale
                            
                            if characterView.upscaling {
                                let candidates: Set<String>
                                
                                if await AgentView.Upscaler.shared.isCompleted {
                                    candidates = Set(characterView.cachedImages.keys).union(pendingPathSet)
                                } else {
                                    candidates = paths
                                }
                                
                                upscaledPaths = Set(candidates.filter { characterView.cachedImages[$0]?.1 == nil })
                            }
                            
                            let loadedImages: ([String: (CGImage, CGImage?)], Double)
                            
                            if !paths.isEmpty || !upscaledPaths.isEmpty {
                                let baseUrl = URL(filePath: characterPath).deletingLastPathComponent()
                                let screenScale = Int(round(displayScale))
                                
                                loadedImages = await Task.detached { @Sendable [paths, upscaledPaths, baseUrl, screenScale, displayScale, imageScale] in
                                    var sources = [(baseUrl, false, paths.union(upscaledPaths))]
                                    var selectedPaths = [String: String]()
                                    var loadedImages = [String: [(CGImage, Bool)]]()
                                    var cachedImages = [String: (CGImage, CGImage?)]()
                                    
                                    if !upscaledPaths.isEmpty, let documents = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {
                                        sources.append((documents.appending(path: baseUrl.lastPathComponent, directoryHint: .isDirectory), true, upscaledPaths))
                                    }
                                    
                                    for (source, upscaling, pathSet) in sources {
                                        for relativePath in pathSet {
                                            let imageUrl = source.appending(path: relativePath, directoryHint: .inferFromPath)
                                            var candidates = [relativePath]
                                            var image: CGImage? = nil
                                            
                                            if upscaling {
                                                guard let path = selectedPaths[relativePath] else {
                                                    continue
                                                }
                                                
                                                candidates = [path]
                                            } else if screenScale > 1 {
                                                let name = imageUrl.lastPathComponent[imageUrl.lastPathComponent.startIndex..<imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.endIndex, offsetBy: imageUrl.pathExtension.isEmpty ? 0 : -imageUrl.pathExtension.count - 1)]
                                                let filename = "\(name)@\(screenScale)x\(imageUrl.lastPathComponent[imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.startIndex, offsetBy: name.count)..<imageUrl.lastPathComponent.endIndex])"
                                                
                                                candidates.insert(((relativePath as NSString).deletingLastPathComponent as NSString).appendingPathComponent(filename), at: 0)
                                            }
                                            
                                            for candidate in candidates {
                                                let path = source.appending(path: candidate, directoryHint: .inferFromPath).path(percentEncoded: false)
                                                
                                                if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                                    defer {
                                                        try? file.close()
                                                    }
                                                    
                                                    if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                                        for i in 0..<CGImageSourceGetCount(imageSource) {
                                                            image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                                            
                                                            break
                                                        }
                                                    }
                                                }
                                                
                                                if image != nil {
                                                    selectedPaths[relativePath] = candidate
                                                    
                                                    break
                                                }
                                            }
                                            
                                            if let image {
                                                if var value = loadedImages[relativePath] {
                                                    value.append((image, upscaling))
                                                    loadedImages[relativePath] = value
                                                } else {
                                                    loadedImages[relativePath] = [(image, upscaling)]
                                                }
                                            }
                                        }
                                    }
                                    
                                    for (key, value) in loadedImages {
                                        if let baseImage = value.first(where: { !$0.1 })?.0 {
                                            if let upscalingImage = value.first(where: { $0.1 }) {
                                                if Double(upscalingImage.0.width) / Double(baseImage.width) > displayScale {
                                                    let width = Int(Double(baseImage.width) * displayScale)
                                                    let height = Int(Double(baseImage.height) * displayScale)
                                                    
                                                    if let context = CGContext(data: nil, width: width, height: height, bitsPerComponent: 8, bytesPerRow: 0, space: CGColorSpace(name: CGColorSpace.sRGB)!, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) {
                                                        
                                                        context.interpolationQuality = .high
                                                        context.draw(upscalingImage.0, in: CGRect(x: 0, y: 0, width: width, height: height))
                                                        cachedImages[key] = (baseImage, context.makeImage())
                                                    } else {
                                                        cachedImages[key] = (baseImage, nil)
                                                    }
                                                } else {
                                                    cachedImages[key] = (baseImage, upscalingImage.0)
                                                }
                                            } else if paths.contains(key) {
                                                cachedImages[key] = (baseImage, nil)
                                            }
                                        }
                                    }
                                    
                                    if let sample = cachedImages.values.first(where: { $0.1 != nil }), let image = sample.1 {
                                        return (cachedImages, Double(image.width) / Double(sample.0.width))
                                    }
                                    
                                    return (cachedImages, imageScale)
                                }.value
                            } else {
                                loadedImages = ([String: (CGImage, CGImage?)](), imageScale)
                            }
                            
                            let characterViews = self.characterViews
                            
                            guard characterViews.contains(where: { $0 === characterView }) else {
                                return
                            }
                            
                            let timelines = pendingTimelines
                            var previousTimelines = [Timeline]()
                            var minZIndex = pendingMinZIndex
                            var maxZIndex = pendingMaxZIndex
                            let zIndexSet = pendingZIndexSet
                            var pathSet = pendingPathSet
                            var selectedTypes = Set<String>()
                            var keySet = Set<String>()
                            var cachedTimelines = [Timeline]()
                            var currentTypes = Set<String>()
                            var stageRequired = false
                            
                            for (path, image) in loadedImages.0 {
                                characterView.cachedImages[path] = (image.0, image.1 ?? characterView.cachedImages[path]?.1)
                            }
                            
                            if loadedImages.1 != characterView.imageScale || !loadedImages.0.isEmpty {
                                characterView.isInvalidated = true
                            }
                            
                            characterView.imageScale = loadedImages.1
                            
                            for timeline in characterView.cachedTimelines {
                                if !zIndexSet.contains(timeline.animation.z) {
                                    if timeline.animation.z < minZIndex {
                                        minZIndex = timeline.animation.z
                                    }
                                    
                                    if timeline.animation.z > maxZIndex {
                                        maxZIndex = timeline.animation.z
                                    }
                                    
                                    for sprite in timeline.animation {
                                        if let path = sprite.path, !path.isEmpty && !pathSet.contains(path) {
                                            pathSet.insert(path)
                                        }
                                    }
                                    
                                    previousTimelines.append(timeline)
                                }
                                
                                if let type = timeline.animation.type, !selectedTypes.contains(type) && characterView.fades["\(timeline.animation.z)&\(type)"] != nil {
                                    selectedTypes.insert(type)
                                }
                            }
                            
                            for (key, _) in characterView.cachedImages {
                                if !pathSet.contains(key) {
                                    keySet.insert(key)
                                }
                            }
                            
                            characterView.maxDuration = 0.0
                            characterView.nextTimelines.removeAll()
                            
                            for i in minZIndex...maxZIndex {
                                var timelines1 = [Timeline]()
                                var timelines2 = [Timeline]()
                                
                                for timeline in previousTimelines {
                                    if i == timeline.animation.z {
                                        timelines1.append(timeline)
                                    }
                                }
                                
                                for timeline in timelines {
                                    if i == timeline.animation.z {
                                        timelines2.append(timeline)
                                    }
                                }
                                
                                for timeline1 in timelines1 {
                                    if timelines2.isEmpty {
                                        var frames = [Sprite]()
                                        
                                        for sprite in timeline1.animation {
                                            frames.append(sprite)
                                        }
                                        
                                        let timeline = Timeline(animation: Animation(frames: frames))
                                        
                                        timeline.animation.repeats = timeline1.animation.repeats
                                        timeline.animation.z = timeline1.animation.z
                                        timeline.animation.type = timeline1.animation.type
                                        timeline.time = timeline1.time
                                        
                                        cachedTimelines.append(timeline)
                                    } else {
                                        var timeline: Timeline? = nil
                                        var nextTimeline: Timeline? = nil
                                        let index = timelines2.firstIndex(where: { $0.animation.type == timeline1.animation.type })
                                        
                                        if let index {
                                            let timeline2 = timelines2[index]
                                            var frames = [Sprite]()
                                            
                                            for sprite in timeline2.animation {
                                                if sprite.delay <= 0.01 {
                                                    var tempSprite = sprite
                                                    
                                                    tempSprite.delay = 0.1
                                                    frames.append(tempSprite)
                                                } else {
                                                    frames.append(sprite)
                                                }
                                            }
                                            
                                            if timeline1 !== timeline2 && timeline1.time > 0.0 && timeline1.time < timeline1.duration {
                                                nextTimeline = Timeline(animation: Animation(frames: frames))
                                                nextTimeline!.animation.repeats = timeline2.animation.repeats
                                                nextTimeline!.animation.z = timeline2.animation.z
                                                nextTimeline!.animation.type = timeline2.animation.type
                                                nextTimeline!.time = timeline2.time
                                            } else {
                                                timeline = Timeline(animation: Animation(frames: frames))
                                                timeline!.animation.repeats = timeline2.animation.repeats
                                                timeline!.animation.z = timeline2.animation.z
                                                timeline!.animation.type = timeline2.animation.type
                                                timeline!.time = timeline2.time
                                            }
                                            
                                            timelines2.remove(at: index)
                                            
                                            if timeline == nil {
                                                var frames = [Sprite]()
                                                
                                                for sprite in timeline1.animation {
                                                    frames.append(sprite)
                                                }
                                                
                                                timeline = Timeline(animation: Animation(frames: frames))
                                                timeline!.animation.repeats = timeline1.animation.repeats
                                                timeline!.animation.z = timeline1.animation.z
                                                timeline!.animation.type = timeline1.animation.type
                                                timeline!.time = timeline1.time
                                            }
                                            
                                            if let nextTimeline {
                                                characterView.nextTimelines.updateValue(nextTimeline, forKey: timeline!)
                                            }
                                            
                                            cachedTimelines.append(timeline!)
                                        }
                                    }
                                }
                                
                                for timeline2 in timelines2 {
                                    var frames = [Sprite]()
                                    
                                    for sprite in timeline2.animation {
                                        if sprite.delay <= 0.01 {
                                            var tempSprite = sprite
                                            
                                            tempSprite.delay = 0.1
                                            frames.append(tempSprite)
                                        } else {
                                            frames.append(sprite)
                                        }
                                    }
                                    
                                    let timeline = Timeline(animation: Animation(frames: frames))
                                    
                                    timeline.animation.repeats = timeline2.animation.repeats
                                    timeline.animation.z = timeline2.animation.z
                                    timeline.animation.type = timeline2.animation.type
                                    timeline.time = timeline2.time
                                    
                                    cachedTimelines.append(timeline)
                                }
                            }
                            
                            for timeline in cachedTimelines {
                                var duration: Double
                                
                                if let type = timeline.animation.type, !currentTypes.contains(type) {
                                    currentTypes.insert(type)
                                }
                                
                                if let nextTimeline = characterView.nextTimelines[timeline] {
                                    duration = timeline.duration - timeline.time
                                    
                                    if nextTimeline.animation.repeats > 0 {
                                        duration += nextTimeline.duration
                                    } else if !nextTimeline.animation.isEmpty {
                                        duration += nextTimeline.animation.first!.delay
                                    }
                                } else if timeline.animation.repeats > 0 {
                                    duration = timeline.duration
                                } else if timeline.animation.isEmpty {
                                    duration = 0.0
                                } else {
                                    duration = timeline.animation.first!.delay
                                }
                                
                                if duration > characterView.maxDuration {
                                    characterView.maxDuration = duration
                                }
                            }
                            
                            for type in selectedTypes {
                                if !currentTypes.contains(type), let value = characterView.types[type] {
                                    characterView.types[type] = (value.0, false, value.2)
                                    stageRequired = true
                                }
                            }
                            
                            if stageRequired {
                                characterView.isInvalidated = true
                                characterView.elapsedTime = characterView.maxDuration
                                characterView.stagingTimelines.append(contentsOf: cachedTimelines)
                            } else {
                                for key in keySet {
                                    characterView.cachedImages.removeValue(forKey: key)
                                }
                                
                                if characterView.cachedTimelines.count != cachedTimelines.count || zip(characterView.cachedTimelines, cachedTimelines).contains(where: { previous, current in
                                    previous.animation.z != current.animation.z || previous.animation.type != current.animation.type
                                }) {
                                    characterView.isInvalidated = true
                                }
                                
                                characterView.elapsedTime = 0.0
                                characterView.cachedTimelines.removeAll()
                                characterView.cachedTimelines.append(contentsOf: cachedTimelines)
                            }
                            
                            characterView.isLoaded = true
                            completed = true
                        }
                    }
                }
            }
        }
        
        return completed
    }
    
    private func snip(image: CGImage) async {
        await Task.detached {
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
                
                if cropWidth > 0 && cropHeight > 0, let croppedImage = image.cropping(to: CGRect(x: minX, y: minY, width: cropWidth, height: cropHeight)), let containerUrl = FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: "group.com.milchchan.Apricot"), let mutableData = CFDataCreateMutable(nil, 0), let destination = CGImageDestinationCreateWithData(mutableData, UTType.png.identifier as CFString, 1, nil) {
                    let uuid = UUID().uuidString
                    
                    CGImageDestinationAddImage(destination, croppedImage, nil)
                    CGImageDestinationFinalize(destination)
                    
                    FileManager.default.createFile(atPath: containerUrl.appending(path: uuid, directoryHint: .inferFromPath).path(percentEncoded: false), contents: mutableData as Data, attributes: nil)
                    
                    if let urls = try? FileManager.default.contentsOfDirectory(at: containerUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
                        var queue = [(URL, String)]()
                        
                        for url in urls {
                            if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), let isDirectory = values.isDirectory, !isDirectory, let name = values.name, UUID(uuidString: name) != nil {
                                queue.append((url, name))
                            }
                        }
                        
                        queue.shuffle()
                        
                        while queue.count > 10 {
                            let (url, name) = queue.removeFirst()
                            
                            if uuid != name {
                                try? FileManager.default.removeItem(atPath: url.path(percentEncoded: false))
                            }
                        }
                    }
                }
            }
        }.value
    }
    
    private func getColor(image: CGImage, scale: CGFloat, x: Int, y: Int) -> (red: CGFloat, green: CGFloat, blue: CGFloat, alpha: CGFloat) {
        var alpha: CGFloat = 0.0
        var red: CGFloat = 0.0
        var green: CGFloat = 0.0
        var blue: CGFloat = 0.0
        
        if let dataProvider = image.dataProvider {
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
                let length = Int(round(scale))
                
                for i in 0..<length {
                    for j in 0..<length {
                        let offset = (y * length + i) * image.bytesPerRow + (x * length + j) * channels
                        
                        alpha += CGFloat(bytes[offset + index.alpha]) / CGFloat(image.bitsPerPixel * 8 - 1)
                        red += CGFloat(bytes[offset + index.red]) / CGFloat(image.bitsPerPixel * 8 - 1)
                        green += CGFloat(bytes[offset + index.green]) / CGFloat(image.bitsPerPixel * 8 - 1)
                        blue += CGFloat(bytes[offset + index.blue]) / CGFloat(image.bitsPerPixel * 8 - 1)
                    }
                }
                
                let pixels = CGFloat(scale * scale)
                
                alpha /= pixels
                red /= pixels
                green /= pixels
                blue /= pixels
            }
        }
        
        return (red: red, green: green, blue: blue, alpha: alpha)
    }
    
    func animationDidStop(_ anim: CAAnimation, finished flag: Bool) {
        for characterView in self.characterViews {
            for view in characterView.subviews {
                if anim === view.layer.animation(forKey: "notify") {
                    for constraint in characterView.constraints {
                        if constraint.firstItem === view || constraint.secondItem === view {
                            characterView.removeConstraint(constraint)
                        }
                    }
                    
                    view.layer.opacity = 0.0
                    view.removeFromSuperview()
                    view.layer.removeAllAnimations()
                    
                    return
                }
            }
        }
    }
    
    func audioPlayerDidFinishPlaying(_ player: AVAudioPlayer, successfully flag: Bool) {
        self.audioPlayer = nil
    }
    
    func audioPlayerDecodeErrorDidOccur(_ player: AVAudioPlayer, error: (any Error)?) {
        self.audioPlayer = nil
    }
    
    class CharacterView: UIScrollView, UIScrollViewDelegate, @MainActor AVAudioPlayerDelegate, @MainActor CLLocationManagerDelegate {
        private var feedbackGenerator: UIImpactFeedbackGenerator? = nil
        weak var parentView: AgentView? = nil
        var contentView: UIView = UIView()
        var balloonView: UIView? = nil
        var name: String? = nil
        var path: String? = nil
        var fades = [String: Double]()
        var types = [String: (Int, Bool, Set<Int>)]()
        var origin: CGPoint = CGPoint.zero
        var size: CGSize = CGSize.zero
        var contentInsets = NSDirectionalEdgeInsets()
        var scale = 1.0
        var upscaling = true
        var imageScale = 1.0
        var language: String? = nil
        var elapsedTime: CFTimeInterval = 0.0
        var maxDuration: CFTimeInterval = 0.0
        var cachedTimelines = [Timeline]()
        var cachedImages = [String: (CGImage, CGImage?)]()
        var sprites = [Sprite]()
        var stagingTimelines = [Timeline]()
        var nextTimelines = [Timeline: Timeline]()
        var stepQueue = [(String, Sequence.Step)]()
        var timelineQueue = [Timeline]()
        var lastIdleDate: Date? = nil
        var lastTickDate: Date = Date()
        var touch: UITouch? = nil
        var audioPlayer: AVAudioPlayer? = nil
        var locationManager: CLLocationManager? = nil
        var isInvalidated = false
        var isLoaded = true
        var isMirror = false
        var maxLines = 5
        var messageQueue = [(step: Double?, index: Int, lines: [(labels: [UILabel], text: String, breaks: Set<Int>, step: Double?, type: (elapsed: Double, speed: Double, buffer: String, count: Int), current: String)], time: Double, speed: Double, duration: Double, slide: (index: Int, step: Double?), reverse: Bool, attributes: [(start: Int, end: Int)], source: Message)]()
        
        override init(frame: CGRect) {
            super.init(frame: frame)
            
            let wrapView = UIView()
            
            self.delegate = self
            self.backgroundColor = .clear
            self.isOpaque = false
            self.isUserInteractionEnabled = true
            self.isScrollEnabled = true
            self.isPagingEnabled = false
            self.alwaysBounceVertical = true
            self.alwaysBounceHorizontal = false
            self.showsVerticalScrollIndicator = false
            self.showsHorizontalScrollIndicator = false
            self.indicatorStyle = .default
            self.clipsToBounds = false
            self.refreshControl = UIRefreshControl()
            self.refreshControl!.tintColor = UIColor { $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0) }
            self.refreshControl!.addTarget(self, action: #selector(self.refreshOccured), for: .valueChanged)
            self.refreshControl!.transform = CGAffineTransformRotate(CGAffineTransformMakeScale(-1.0, 1.0), .pi)
            self.transform = CGAffineTransformMakeScale(1.0, -1.0)
            
            wrapView.translatesAutoresizingMaskIntoConstraints = false
            wrapView.backgroundColor = .clear
            wrapView.isOpaque = false
            wrapView.isUserInteractionEnabled = true
            wrapView.transform = CGAffineTransformMakeScale(1.0, -1.0)
            
            self.contentView.translatesAutoresizingMaskIntoConstraints = false
            self.contentView.backgroundColor = .clear
            self.contentView.isOpaque = false
            self.contentView.isUserInteractionEnabled = true
            
            wrapView.addSubview(self.contentView)
            self.addSubview(wrapView)
            
            self.addConstraint(NSLayoutConstraint(item: wrapView, attribute: .centerX, relatedBy: .equal, toItem: self, attribute: .centerX, multiplier: 1.0, constant: 0.0))
            self.addConstraint(NSLayoutConstraint(item: wrapView, attribute: .top, relatedBy: .equal, toItem: self, attribute: .top, multiplier: 1.0, constant: 0.0))
            self.addConstraint(NSLayoutConstraint(item: wrapView, attribute: .width, relatedBy: .equal, toItem: self.contentView, attribute: .width, multiplier: 1.0, constant: 0.0))
            self.addConstraint(NSLayoutConstraint(item: wrapView, attribute: .height, relatedBy: .equal, toItem: self.contentView, attribute: .height, multiplier: 1.0, constant: 0.0))
            self.addConstraint(NSLayoutConstraint(item: self.contentView, attribute: .centerX, relatedBy: .equal, toItem: wrapView, attribute: .centerX, multiplier: 1.0, constant: 0.0))
            self.addConstraint(NSLayoutConstraint(item: self.contentView, attribute: .bottom, relatedBy: .equal, toItem: wrapView, attribute: .bottom, multiplier: 1.0, constant: 0.0))
            self.addConstraint(NSLayoutConstraint(item: self.contentView, attribute: .width, relatedBy: .equal, toItem: self, attribute: .width, multiplier: 1.0, constant: 0.0))
            self.addConstraint(NSLayoutConstraint(item: self.contentView, attribute: .height, relatedBy: .equal, toItem: self, attribute: .height, multiplier: 1.0, constant: 0.0))
            
            self.locationManager = CLLocationManager()
            self.locationManager!.delegate = self
            self.locationManager!.desiredAccuracy = kCLLocationAccuracyBest
            self.locationManager!.distanceFilter = kCLDistanceFilterNone
        }
        
        required init?(coder aDecoder: NSCoder) {
            super.init(coder: aDecoder)
        }
        
        func show(message: Message) {
            guard let parentView = self.parentView else {
                return
            }
            
            let safeBounds = parentView.bounds.inset(by: parentView.safeAreaInsets)
            
            if safeBounds.width > 0.0 && safeBounds.height > 0.0 {
                let font = UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize, weight: .bold)
                let lineHeight = ceil(font.lineHeight * 1.5)
                let messageWidth = floor(self.traitCollection.horizontalSizeClass == .compact && self.traitCollection.verticalSizeClass == .regular && safeBounds.width < safeBounds.height ? safeBounds.width - 32.0 : safeBounds.width / Double(parentView.characterViews.count <= 2 ? 2 : parentView.characterViews.count) - 32.0)
                let radius = lineHeight
                let maxLineWidth = messageWidth - radius * 2.0
                
                if maxLineWidth > 0.0 {
                    for subview in self.balloonView!.subviews {
                        if let visualEffectView = subview as? UIVisualEffectView {
                            var content = String()
                            var index = 0
                            var i = 0
                            var current = String()
                            var text = String()
                            var breaks = Set<Int>()
                            var lines = [(labels: [UILabel], text: String, breaks: Set<Int>, step: Double?, type: (elapsed: Double, speed: Double, buffer: String, count: Int), current: String)]()
                            var count = 0
                            var attributes = [(start: Int, end: Int)]()
                            let balloonPartSize = CGSizeMake(11.0, 11.0)
                            let maskPath = CGMutablePath()
                            let accentColor = parentView.accentColor ?? UIColor(named: "AccentColor")!
                            let language: [(NSAttributedString.Key, Any)] = {
                                if let language = self.language {
                                    return [(.languageIdentifier, language)]
                                }
                                
                                return []
                            }()
                            let messageView = UIView()
                            let swipeRightGestureRecognizer = UISwipeGestureRecognizer(target: self, action: #selector(self.swiped))
                            let swipeLeftGestureRecognizer = UISwipeGestureRecognizer(target: self, action: #selector(self.swiped))
                            
                            swipeRightGestureRecognizer.direction = .right
                            swipeLeftGestureRecognizer.direction = .left
                            
                            messageView.translatesAutoresizingMaskIntoConstraints = false
                            messageView.isUserInteractionEnabled = true
                            messageView.backgroundColor = .clear
                            messageView.clipsToBounds = true
                            messageView.addGestureRecognizer(swipeRightGestureRecognizer)
                            messageView.addGestureRecognizer(swipeLeftGestureRecognizer)
                            
                            visualEffectView.contentView.insertSubview(messageView, at: count)
                            
                            for inline in message {
                                if inline.attributes == nil {
                                    content.append(inline.text)
                                    index += inline.text.count
                                } else {
                                    let s = inline.text.filter { !$0.isNewline }
                                    
                                    content.append(s)
                                    attributes.append((start: index, end: index + s.count))
                                    index += s.count
                                }
                            }
                            
                            while i < content.count {
                                let character = content[content.index(content.startIndex, offsetBy: i)]
                                
                                if character.isNewline {
                                    var labels = [UILabel]()
                                    
                                    for _ in 0..<breaks.count + 1 {
                                        let messageLabel = UILabel(frame: CGRect.zero)
                                        let maskLayer = CAShapeLayer()
                                        
                                        maskLayer.fillRule = .evenOdd
                                        maskLayer.strokeColor = UIColor.clear.cgColor
                                        maskLayer.lineWidth = 0.0
                                        maskLayer.fillColor = UIColor(white: 1.0, alpha: 1.0).cgColor
                                        maskLayer.path = CGPath(rect: CGRect(x: 0.0, y: 0.0, width: maxLineWidth, height: ceil(font.lineHeight)), transform: nil)
                                        
                                        messageLabel.translatesAutoresizingMaskIntoConstraints = false
                                        messageLabel.isUserInteractionEnabled = false
                                        messageLabel.backgroundColor = .clear
                                        messageLabel.contentMode = .topLeft
                                        messageLabel.font = font
                                        messageLabel.lineBreakMode = .byClipping
                                        messageLabel.numberOfLines = 1
                                        messageLabel.transform = CGAffineTransformMakeTranslation(0.0, 0.0)
                                        messageLabel.layer.mask = maskLayer
                                        
                                        messageView.insertSubview(messageLabel, at: count)
                                        
                                        messageView.addConstraint(NSLayoutConstraint(item: messageLabel, attribute: .leading, relatedBy: .equal, toItem: messageView, attribute: .leading, multiplier: 1.0, constant: 0.0))
                                        messageView.addConstraint(NSLayoutConstraint(item: messageLabel, attribute: .top, relatedBy: .equal, toItem: messageView, attribute: .top, multiplier: 1.0, constant: lineHeight * Double(count)))
                                        messageView.addConstraint(NSLayoutConstraint(item: messageLabel, attribute: .height, relatedBy: .equal, toItem: nil, attribute: .notAnAttribute, multiplier: 1.0, constant: ceil(font.lineHeight)))
                                        
                                        labels.append(messageLabel)
                                        count += 1
                                    }
                                    
                                    lines.append((labels: labels, text: text, breaks: breaks, step: nil, type: (elapsed: -1.0, speed: message.speed, buffer: String(), count: 0), current: String()))
                                    
                                    if i == content.count - 1 {
                                        let messageLabel = UILabel(frame: CGRect.zero)
                                        let maskLayer = CAShapeLayer()
                                        
                                        maskLayer.fillRule = .evenOdd
                                        maskLayer.strokeColor = UIColor.clear.cgColor
                                        maskLayer.lineWidth = 0.0
                                        maskLayer.fillColor = UIColor(white: 1.0, alpha: 1.0).cgColor
                                        maskLayer.path = CGPath(rect: CGRect(x: 0.0, y: 0.0, width: maxLineWidth, height: ceil(font.lineHeight)), transform: nil)
                                        
                                        messageLabel.translatesAutoresizingMaskIntoConstraints = false
                                        messageLabel.isUserInteractionEnabled = false
                                        messageLabel.backgroundColor = .clear
                                        messageLabel.contentMode = .topLeft
                                        messageLabel.font = font
                                        messageLabel.lineBreakMode = .byClipping
                                        messageLabel.numberOfLines = 1
                                        messageLabel.transform = CGAffineTransformMakeTranslation(0.0, 0.0)
                                        messageLabel.layer.mask = maskLayer
                                        
                                        messageView.insertSubview(messageLabel, at: count)
                                        
                                        messageView.addConstraint(NSLayoutConstraint(item: messageLabel, attribute: .leading, relatedBy: .equal, toItem: messageView, attribute: .leading, multiplier: 1.0, constant: 0.0))
                                        messageView.addConstraint(NSLayoutConstraint(item: messageLabel, attribute: .top, relatedBy: .equal, toItem: messageView, attribute: .top, multiplier: 1.0, constant: lineHeight * Double(count)))
                                        messageView.addConstraint(NSLayoutConstraint(item: messageLabel, attribute: .height, relatedBy: .equal, toItem: nil, attribute: .notAnAttribute, multiplier: 1.0, constant: ceil(font.lineHeight)))
                                        
                                        count += 1
                                        lines.append((labels: [messageLabel], text: String(), breaks: [], step: nil, type: (elapsed: -1.0, speed: message.speed, buffer: String(), count: 0), current: String()))
                                        
                                        break
                                    }
                                    
                                    current.removeAll()
                                    text.removeAll()
                                    breaks.removeAll()
                                } else if character.isWhitespace {
                                    if current.isEmpty {
                                        i += 1
                                        
                                        continue
                                    }
                                    
                                    current.append(character)
                                    text.append(character)
                                    
                                    let offset = lines.reduce(0, { $0 + $1.text.count })
                                    var components: [(text: String, highlight: Int?)] = [(text: String(current[current.startIndex]), highlight: attributes.firstIndex(where: { offset >= $0.start && offset < $0.end }))]
                                    let mutableAttributedString = NSMutableAttributedString()
                                    let paragraphStyle = NSMutableParagraphStyle()
                                    
                                    paragraphStyle.minimumLineHeight = font.lineHeight
                                    paragraphStyle.maximumLineHeight = font.lineHeight
                                    
                                    for j in 1..<current.count {
                                        let highlight = attributes.firstIndex(where: { offset + j >= $0.start && offset + j < $0.end })
                                        var component = components[components.count - 1]
                                        
                                        if highlight == component.highlight {
                                            component.text.append(current[current.index(current.startIndex, offsetBy: j)])
                                            components[components.count - 1] = component
                                        } else {
                                            components.append((text: String(current[current.index(current.startIndex, offsetBy: j)]), highlight: highlight))
                                        }
                                    }
                                    
                                    for component in components {
                                        mutableAttributedString.append(NSAttributedString(string: component.text, attributes: Swift.Dictionary(uniqueKeysWithValues: [(.font, font), (.foregroundColor, component.highlight == nil ? UIColor { $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0) } : accentColor), (.paragraphStyle, paragraphStyle)] + language)))
                                    }
                                    
                                    if mutableAttributedString.boundingRect(with: CGSizeMake(CGFLOAT_MAX, CGFLOAT_MAX), options: .usesLineFragmentOrigin, context: nil).width > maxLineWidth {
                                        current.removeSubrange(current.startIndex..<current.endIndex)
                                        text.remove(at: text.index(text.endIndex, offsetBy: -1))
                                        breaks.insert(text.count)
                                    }
                                } else {
                                    current.append(character)
                                    text.append(character)
                                    
                                    let offset = lines.reduce(0, { $0 + $1.text.count })
                                    var components: [(text: String, highlight: Int?)] = [(text: String(current[current.startIndex]), highlight: attributes.firstIndex(where: { offset >= $0.start && offset < $0.end }))]
                                    let mutableAttributedString = NSMutableAttributedString()
                                    let paragraphStyle = NSMutableParagraphStyle()
                                    
                                    paragraphStyle.minimumLineHeight = font.lineHeight
                                    paragraphStyle.maximumLineHeight = font.lineHeight
                                    
                                    for j in 1..<current.count {
                                        let highlight = attributes.firstIndex(where: { offset + j >= $0.start && offset + j < $0.end })
                                        var component = components[components.count - 1]
                                        
                                        if highlight == component.highlight {
                                            component.text.append(current[current.index(current.startIndex, offsetBy: j)])
                                            components[components.count - 1] = component
                                        } else {
                                            components.append((text: String(current[current.index(current.startIndex, offsetBy: j)]), highlight: highlight))
                                        }
                                    }
                                    
                                    for component in components {
                                        mutableAttributedString.append(NSAttributedString(string: component.text, attributes: Swift.Dictionary(uniqueKeysWithValues: [(.font, font), (.foregroundColor, component.highlight == nil ? UIColor { $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0) } : accentColor), (.paragraphStyle, paragraphStyle)] + language)))
                                    }
                                    
                                    if mutableAttributedString.boundingRect(with: CGSizeMake(CGFLOAT_MAX, CGFLOAT_MAX), options: .usesLineFragmentOrigin, context: nil).width > maxLineWidth {
                                        var spaceIndex: Int? = nil
                                        
                                        for j in stride(from: current.count - 1, through: 0, by: -1) {
                                            let c = current[current.index(current.startIndex, offsetBy: j)]
                                            
                                            if c.isWhitespace {
                                                spaceIndex = j
                                                
                                                break
                                            } else if !c.isASCII {
                                                break
                                            }
                                        }
                                        
                                        if let spaceIndex {
                                            let distance = -spaceIndex + current.count - 1
                                            
                                            current.removeSubrange(current.startIndex..<current.index(current.endIndex, offsetBy: -distance))
                                            breaks.insert(text.count - distance)
                                        } else {
                                            current.removeSubrange(current.startIndex..<current.index(current.endIndex, offsetBy: -1))
                                            breaks.insert(text.count - 1)
                                        }
                                    }
                                }
                                
                                i += 1
                            }
                            
                            if !text.isEmpty {
                                var labels = [UILabel]()
                                
                                for _ in 0..<breaks.count + 1 {
                                    let messageLabel = UILabel(frame: CGRect.zero)
                                    let maskLayer = CAShapeLayer()
                                    
                                    maskLayer.fillRule = .evenOdd
                                    maskLayer.strokeColor = UIColor.clear.cgColor
                                    maskLayer.lineWidth = 0.0
                                    maskLayer.fillColor = UIColor(white: 1.0, alpha: 1.0).cgColor
                                    maskLayer.path = CGPath(rect: CGRect(x: 0.0, y: 0.0, width: maxLineWidth, height: ceil(font.lineHeight)), transform: nil)
                                    
                                    messageLabel.translatesAutoresizingMaskIntoConstraints = false
                                    messageLabel.isUserInteractionEnabled = false
                                    messageLabel.backgroundColor = .clear
                                    messageLabel.contentMode = .topLeft
                                    messageLabel.font = font
                                    messageLabel.lineBreakMode = .byClipping
                                    messageLabel.numberOfLines = 1
                                    messageLabel.transform = CGAffineTransformMakeTranslation(0.0, 0.0)
                                    messageLabel.layer.mask = maskLayer
                                    
                                    messageView.insertSubview(messageLabel, at: count)
                                    
                                    messageView.addConstraint(NSLayoutConstraint(item: messageLabel, attribute: .leading, relatedBy: .equal, toItem: messageView, attribute: .leading, multiplier: 1.0, constant: 0.0))
                                    messageView.addConstraint(NSLayoutConstraint(item: messageLabel, attribute: .top, relatedBy: .equal, toItem: messageView, attribute: .top, multiplier: 1.0, constant: lineHeight * Double(count)))
                                    messageView.addConstraint(NSLayoutConstraint(item: messageLabel, attribute: .height, relatedBy: .equal, toItem: nil, attribute: .notAnAttribute, multiplier: 1.0, constant: ceil(font.lineHeight)))
                                    
                                    labels.append(messageLabel)
                                    count += 1
                                }
                                
                                lines.append((labels: labels, text: text, breaks: breaks, step: nil, type: (elapsed: -1.0, speed: message.speed, buffer: String(), count: 0), current: String()))
                            }
                            
                            self.messageQueue.append((step: 0.0, index: 0, lines: lines, time: 0.0, speed: 1.0, duration: message.duration, slide: (index: 0, step: nil), reverse: false, attributes: attributes, source: message))
                            
                            let preferredScale = (self.scale == 0.0 ? self.traitCollection.displayScale : self.scale) * parentView.userScale * parentView.systemScale
                            let frame = CGRect(x: self.origin.x * preferredScale / self.traitCollection.displayScale, y: self.origin.y * preferredScale / self.traitCollection.displayScale, width: self.size.width * preferredScale / self.traitCollection.displayScale, height: self.size.height * preferredScale / self.traitCollection.displayScale)
                            let contentHeight = count > 1 ? font.lineHeight + lineHeight * Double(min(count, self.maxLines) - 1) : font.lineHeight
                            let messageHeight = ceil(radius * 2.0 + contentHeight)
                            let maxScale = (messageWidth + 16.0) / messageWidth
                            let balloonPath = self.createBalloonPath(messageWidth: messageWidth, messageHeight: messageHeight, balloonPartSize: balloonPartSize, radius: radius)
                            let margin = floor((messageHeight + balloonPartSize.height) * maxScale - frame.origin.y)
                            
                            visualEffectView.contentView.addConstraint(NSLayoutConstraint(item: messageView, attribute: .leading, relatedBy: .equal, toItem: visualEffectView.contentView, attribute: .leading, multiplier: 1.0, constant: radius))
                            visualEffectView.contentView.addConstraint(NSLayoutConstraint(item: messageView, attribute: .top, relatedBy: .equal, toItem: visualEffectView.contentView, attribute: .top, multiplier: 1.0, constant: radius))
                            visualEffectView.contentView.addConstraint(NSLayoutConstraint(item: messageView, attribute: .width, relatedBy: .equal, toItem: nil, attribute: .notAnAttribute, multiplier: 1.0, constant: maxLineWidth))
                            visualEffectView.contentView.addConstraint(NSLayoutConstraint(item: messageView, attribute: .height, relatedBy: .equal, toItem: nil, attribute: .notAnAttribute, multiplier: 1.0, constant: ceil(contentHeight)))
                            
                            for constraint in self.constraints {
                                if constraint.firstItem === self {
                                    if constraint.firstAttribute == .width {
                                        constraint.constant = ceil(max(frame.width, messageWidth * maxScale))
                                    } else if constraint.firstAttribute == .height {
                                        constraint.constant = ceil(margin + frame.height)
                                    }
                                } else if constraint.firstItem === self.contentView && constraint.secondItem === self {
                                    if constraint.firstAttribute == .width {
                                        constraint.constant = -floor(max(frame.width, messageWidth * maxScale) - frame.width)
                                    } else if constraint.firstAttribute == .height {
                                        constraint.constant = -margin
                                    }
                                } else if constraint.firstItem === self.balloonView {
                                    if constraint.firstAttribute == .width {
                                        constraint.constant = messageWidth
                                    } else if constraint.firstAttribute == .height {
                                        constraint.constant = messageHeight + balloonPartSize.height
                                    } else if constraint.firstAttribute == .bottom {
                                        constraint.constant = round((messageHeight + balloonPartSize.height) / 2.0 - frame.origin.y)
                                    }
                                }
                            }
                            
                            for constraint in visualEffectView.contentView.constraints {
                                if constraint.firstItem is UIButton && constraint.secondItem === visualEffectView.contentView {
                                    if constraint.firstAttribute == .trailing {
                                        constraint.constant = -radius
                                    } else if constraint.firstAttribute == .bottom {
                                        constraint.constant = -ceil(balloonPartSize.height + radius + font.lineHeight / 2.0)
                                    }
                                }
                            }
                            
                            maskPath.addPath(balloonPath)
                            maskPath.addRect(CGRect(x: -8.0 * 2.0, y: -8.0 * 2.0, width: messageWidth + 8.0 * 4.0, height: messageHeight + balloonPartSize.height + 8.0 * 4.0))
                            maskPath.closeSubpath()
                            
                            if let sublayers = self.balloonView!.layer.sublayers {
                                for sublayer in sublayers {
                                    if let shapeLayer = sublayer as? CAShapeLayer {
                                        shapeLayer.path = balloonPath
                                        shapeLayer.shadowPath = balloonPath
                                        
                                        if let maskLayer = shapeLayer.mask as? CAShapeLayer {
                                            maskLayer.path = maskPath
                                        }
                                    }
                                }
                            }
                            
                            for subview in self.balloonView!.subviews {
                                if let visualEffectView = subview as? UIVisualEffectView, let maskView = visualEffectView.mask, let sublayers = maskView.layer.sublayers {
                                    for sublayer in sublayers {
                                        if let maskLayer = sublayer as? CAShapeLayer {
                                            maskLayer.path = balloonPath
                                        }
                                    }
                                }
                            }
                            
                            self.balloonView!.isHidden = false
                            
                            break
                        }
                    }
                    
                    self.parentView?.delegate?.agentWillSpeak(self.parentView!, message: message)
                }
            }
        }
        
        func scrollViewWillBeginDragging(_ scrollView: UIScrollView) {
            UIView.transition(with: self.contentView, duration: 0.5, options: [.curveEaseIn, .allowUserInteraction], animations: {
                self.contentView.alpha = 0.5
            })
        }
        
        func scrollViewDidEndDragging(_ scrollView: UIScrollView, willDecelerate decelerate: Bool) {
            UIView.transition(with: self.contentView, duration: 0.5, options: [.curveEaseOut, .allowUserInteraction], animations: {
                self.contentView.alpha = 1.0
            })
        }
        
        override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
            if self.touch == nil, let first = touches.first {
                self.touch = first
            }
            
            if self.feedbackGenerator == nil {
                self.feedbackGenerator = UIImpactFeedbackGenerator()
                self.feedbackGenerator!.prepare()
            }
        }
        
        override func layoutSubviews() {
            super.layoutSubviews()
            
            for subview in self.balloonView!.subviews {
                if let visualEffectView = subview as? UIVisualEffectView, let maskView = visualEffectView.mask, maskView.frame != self.balloonView!.bounds {
                    maskView.frame = self.balloonView!.bounds
                    
                    if let sublayers = maskView.layer.sublayers {
                        for sublayer in sublayers {
                            if let maskLayer = sublayer as? CAShapeLayer {
                                maskLayer.frame = maskView.bounds
                            }
                        }
                    }
                }
            }
        }
        
        override func touchesMoved(_ touches: Set<UITouch>, with event: UIEvent?) {
            if let touch = self.touch, touches.contains(touch) {
                let location = touch.location(in: self.contentView)
                let types = self.types.compactMap({ $0.value.1 ? $0.key : nil })
                var state: String? = nil
                
                for timeline in self.cachedTimelines {
                    if !timeline.animation.isEmpty {
                        let current = timeline.current
                        
                        if current.opacity > 0.0, let path = current.path {
                            let key: String
                            var tempTypes: [String?]? = nil
                            var isVisible: Bool
                            let isTouchable: Bool
                            
                            if timeline.animation.type == nil {
                                key = String(timeline.animation.z)
                                tempTypes = []
                                
                                for cachedTimeline in self.cachedTimelines {
                                    if cachedTimeline.animation.z == timeline.animation.z {
                                        tempTypes!.append(cachedTimeline.animation.type)
                                    }
                                }
                            } else {
                                key = "\(timeline.animation.z)&\(timeline.animation.type!)"
                            }
                            
                            let fade = self.fades[key]
                            
                            if let tempTypes {
                                if types.isEmpty {
                                    isVisible = true
                                } else {
                                    isVisible = !tempTypes.contains { type in
                                        if let type {
                                            return types.contains(type)
                                        }
                                        
                                        return false
                                    }
                                }
                            } else if types.isEmpty {
                                isVisible = false
                            } else {
                                tempTypes = []
                                
                                for cachedTimeline in self.cachedTimelines {
                                    if let type = cachedTimeline.animation.type, cachedTimeline.animation.z == timeline.animation.z && types.contains(type) {
                                        tempTypes!.append(type)
                                    }
                                }
                                
                                isVisible = !tempTypes!.isEmpty && tempTypes!.lastIndex(of: timeline.animation.type!) == tempTypes!.count - 1
                            }
                            
                            if isVisible {
                                if let fade {
                                    isTouchable = fade > 0.0
                                } else {
                                    isTouchable = timeline.animation.type == nil
                                }
                            } else if let fade {
                                isTouchable = fade > 0.0
                            } else {
                                isTouchable = false
                            }
                            
                            if isTouchable, let image = self.cachedImages[path]?.0, let parentView = self.parentView {
                                let x = round(self.origin.x + current.location.x)
                                let y = round(self.origin.y + current.location.y)
                                var width = current.size.width
                                var height = current.size.height
                                let scale = (self.scale == 0.0 ? 1.0 : self.scale / self.traitCollection.displayScale) * parentView.userScale * parentView.systemScale
                                
                                if width == 0.0 && height == 0.0 {
                                    width = Double(image.width)
                                    height = Double(image.height)
                                } else if width == 0.0 {
                                    width = height * Double(image.width) / Double(image.height)
                                } else if height == 0.0 {
                                    height = width * Double(image.height) / Double(image.width)
                                }
                                
                                if x * scale <= location.x && location.x < (x + floor(width)) * scale && y * scale <= location.y && location.y < (y + floor(height)) * scale {
                                    state = current.path;
                                }
                            }
                        }
                    }
                }
                
                if let state {
                    if state != Script.shared.states["DoubleClick"] {
                        let sequences = Script.shared.characters.reduce(into: [Sequence](), { x, y in
                            if y.name == self.name {
                                for sequence in y.sequences {
                                    if sequence.name == "DoubleClick", let pattern = sequence.state, let regex = try? Regex(pattern), let match = state.firstMatch(of: regex), !match.output.isEmpty {
                                        x.append(sequence)
                                    }
                                }
                            }
                        })
                        
                        if !sequences.isEmpty {
                            Task {
                                await Script.shared.run(name: self.name!, sequences: sequences, state: state, words: [])
                            }
                            
                            self.feedbackGenerator?.impactOccurred()
                            self.feedbackGenerator?.prepare()
                        }
                    } else if state != Script.shared.states["DoubleTap"] {
                        let sequences = Script.shared.characters.reduce(into: [Sequence](), { x, y in
                            if y.name == self.name {
                                for sequence in y.sequences {
                                    if sequence.name == "DoubleTap", let pattern = sequence.state, let regex = try? Regex(pattern), let match = state.firstMatch(of: regex), !match.output.isEmpty {
                                        x.append(sequence)
                                    }
                                }
                            }
                        })
                        
                        if !sequences.isEmpty {
                            Task {
                                await Script.shared.run(name: self.name!, sequences: sequences, state: state, words: [])
                            }
                            
                            self.feedbackGenerator?.impactOccurred()
                            self.feedbackGenerator?.prepare()
                        }
                    }
                }
                
                self.touch = nil
            }
        }
        
        override func touchesEnded(_ touches: Set<UITouch>, with event: UIEvent?) {
            self.touch = nil
            self.feedbackGenerator = nil
        }
        
        override func touchesCancelled(_ touches: Set<UITouch>, with event: UIEvent?) {
            self.touch = nil
            self.feedbackGenerator = nil
        }
        
        @objc private func swiped(sender: UISwipeGestureRecognizer) {
            if sender.state == .ended {
                for subview in self.balloonView!.subviews {
                    if let visualEffectView = subview as? UIVisualEffectView, visualEffectView.contentView.subviews.contains(where: { view in
                        if let gestureRecognizers = view.gestureRecognizers, gestureRecognizers.contains(sender) {
                            return true
                        }
                        
                        return false
                    }) {
                        if !self.messageQueue.isEmpty {
                            if sender.direction == .right {
                                if !self.messageQueue[0].reverse {
                                    self.messageQueue[0].speed = 2.0
                                }
                            } else if sender.direction == .left {
                                self.messageQueue[0].speed = 2.0
                                self.messageQueue[0].reverse = true
                            }
                        }
                        
                        break
                    }
                }
            }
        }
        
        func refresh() {
            let yesterday = Date(timeIntervalSinceNow: -60 * 60 * 24)
            
            if Script.shared.scores.contains(where: { $0.value.3 > yesterday }) {
                if self.locationManager!.authorizationStatus == .notDetermined || self.locationManager!.authorizationStatus == .denied || self.locationManager!.authorizationStatus == .restricted {
                    WidgetCenter.shared.reloadAllTimelines()
                    
                    self.parentView?.delegate?.agentDidRefresh(self.parentView!)
                } else {
                    self.locationManager!.requestLocation()
                }
            } else {
                self.refreshControl!.sendActions(for: .valueChanged)
            }
        }
        
        @objc private func refreshOccured(sender: UIRefreshControl) {
            Task(priority: .utility) {
                if await Script.shared.update() {
                    await Script.shared.run(name: self.name!, sequences: Script.shared.characters.reduce(into: [], { x, y in
                        if y.name == self.name {
                            for sequence in y.sequences {
                                if sequence.name == "Alert" {
                                    x.append(sequence)
                                }
                            }
                        }
                    }), words: [])
                }
                
                if self.locationManager!.authorizationStatus == .notDetermined || self.locationManager!.authorizationStatus == .denied || self.locationManager!.authorizationStatus == .restricted {
                    WidgetCenter.shared.reloadAllTimelines()
                    
                    if sender.isRefreshing {
                        sender.endRefreshing()
                    }
                    
                    self.parentView?.delegate?.agentDidRefresh(self.parentView!)
                } else {
                    self.locationManager!.requestLocation()
                }
            }
        }
        
        @discardableResult
        func render(timelines: [Timeline], images: [String: CGImage], imageScale: Double, deltaTime: Double) -> (CGImage?, Bool) {
            var image: CGImage? = nil
            var completed = true
            
            if let parentView = self.parentView {
                let format = UIGraphicsImageRendererFormat(for: self.traitCollection)
                
                format.opaque = false
                format.preferredRange = .standard
                format.scale = (self.scale == 0.0 ? self.traitCollection.displayScale : self.scale) * imageScale
                
                let renderer = UIGraphicsImageRenderer(size: self.size, format: format)
                let renderedImage = renderer.image { rendererContext in
                    let context = rendererContext.cgContext
                    let actualScale = parentView.userScale * parentView.systemScale
                    let types = self.types.compactMap({ $0.value.1 ? $0.key : nil })
                    
                    if imageScale == 1.0 && actualScale == floor(actualScale) {
                        context.interpolationQuality = .none
                        context.setAllowsAntialiasing(false)
                    } else {
                        context.interpolationQuality = .high
                        context.setAllowsAntialiasing(true)
                    }
                    
                    context.clear(CGRect(origin: CGPoint.zero, size: self.size))
                    context.translateBy(x: 0.0, y: self.size.height)
                    context.scaleBy(x: 1.0, y: -1.0)
                    
                    for timeline in timelines {
                        if !timeline.animation.isEmpty {
                            let current = timeline.current
                            
                            if current.opacity > 0.0, let path = current.path {
                                let key: String
                                var tempTypes: [String?]? = nil
                                let isVisible: Bool
                                let alpha: Double
                                
                                if timeline.animation.type == nil {
                                    key = String(timeline.animation.z)
                                    tempTypes = []
                                    
                                    for cachedTimeline in timelines {
                                        if cachedTimeline.animation.z == timeline.animation.z {
                                            tempTypes!.append(cachedTimeline.animation.type)
                                        }
                                    }
                                } else {
                                    key = "\(timeline.animation.z)&\(timeline.animation.type!)"
                                }
                                
                                let fade = self.fades[key]
                                
                                if let tempTypes {
                                    if types.isEmpty {
                                        isVisible = true
                                    } else {
                                        isVisible = !tempTypes.contains { type in
                                            if let type {
                                                return types.contains(type)
                                            }
                                            
                                            return false
                                        }
                                    }
                                } else if types.isEmpty {
                                    isVisible = false
                                } else {
                                    tempTypes = []
                                    
                                    for cachedTimeline in timelines {
                                        if let type = cachedTimeline.animation.type, cachedTimeline.animation.z == timeline.animation.z && types.contains(type) {
                                            tempTypes!.append(type)
                                        }
                                    }
                                    
                                    isVisible = !tempTypes!.isEmpty && tempTypes!.lastIndex(of: timeline.animation.type!) == tempTypes!.count - 1
                                }
                                
                                if isVisible {
                                    if fade != nil && timeline.animation.type == nil || timeline.animation.type != nil {
                                        let step = fade == nil ? deltaTime : fade! + deltaTime
                                        
                                        if step >= 1.0 {
                                            if timeline.animation.type == nil {
                                                self.fades.removeValue(forKey: key)
                                            } else {
                                                self.fades[key] = 1.0
                                            }
                                            
                                            alpha = 1.0
                                        } else {
                                            self.fades[key] = step
                                            alpha = sin(step / 2.0 * .pi)
                                            completed = false
                                        }
                                    } else {
                                        alpha = 1.0
                                    }
                                } else if (fade != nil || timeline.animation.type != nil) && fade == nil {
                                    alpha = 0.0
                                } else {
                                    let step = fade == nil && timeline.animation.type == nil ? 1.0 - deltaTime : fade! - deltaTime
                                    
                                    if step <= 0.0 {
                                        if timeline.animation.type == nil {
                                            self.fades[key] = 0.0
                                        } else {
                                            self.fades.removeValue(forKey: key)
                                        }
                                        
                                        alpha = 0.0
                                    } else {
                                        self.fades[key] = step
                                        alpha = sin(step / 2.0 * .pi)
                                        completed = false
                                    }
                                }
                                
                                if alpha > 0.0, let i = images[path] {
                                    let width: Double
                                    let height: Double
                                    
                                    if current.size.width == 0.0 && current.size.height == 0.0 {
                                        width = floor(Double(i.width) / imageScale)
                                        height = floor(Double(i.height) / imageScale)
                                    } else if current.size.width == 0.0 {
                                        width = floor(current.size.height * Double(i.width) / Double(i.height))
                                        height = floor(current.size.height)
                                    } else if current.size.height == 0.0 {
                                        width = floor(current.size.width)
                                        height = floor(current.size.width * Double(i.height) / Double(i.width))
                                    } else {
                                        width = floor(current.size.width)
                                        height = floor(current.size.height)
                                    }
                                    
                                    context.saveGState()
                                    context.concatenate(CGAffineTransformMakeTranslation(round(self.origin.x + current.location.x), round(self.size.height - self.origin.y - current.location.y - height)))
                                    context.setAlpha(current.opacity * alpha)
                                    context.draw(i, in: CGRect(x: 0.0, y: 0.0, width: width, height: height))
                                    context.restoreGState()
                                }
                            }
                        }
                    }
                }
                
                image = renderedImage.cgImage
            }
            
            if let image, let parentView = self.parentView {
                let scale = (self.scale == 0.0 ? 1.0 : self.scale / self.traitCollection.displayScale) * parentView.userScale * parentView.systemScale
                let size = CGSize(width: ceil(self.size.width * scale), height: ceil(self.size.height * scale))
                let format = UIGraphicsImageRendererFormat(for: self.traitCollection)
                
                format.opaque = false
                format.preferredRange = .standard
                format.scale = self.traitCollection.displayScale
                
                let renderer = UIGraphicsImageRenderer(size: size, format: format)
                let renderedImage = renderer.image { rendererContext in
                    let context = rendererContext.cgContext
                    
                    context.interpolationQuality = .high
                    context.setAllowsAntialiasing(true)
                    context.clear(CGRect(origin: CGPoint.zero, size: size))
                    
                    if self.isMirror {
                        context.translateBy(x: size.width, y: size.height)
                        context.scaleBy(x: -1.0, y: -1.0)
                    } else {
                        context.translateBy(x: 0, y: size.height)
                        context.scaleBy(x: 1.0, y: -1.0)
                    }
                    
                    context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: size.width, height: size.height))
                }
                
                if let image = renderedImage.cgImage {
                    CATransaction.begin()
                    CATransaction.setDisableActions(true)
                    
                    self.contentView.layer.contents = image
                    
                    CATransaction.commit()
                }
            }
            
            return (image, completed)
        }
        
        @discardableResult
        func preview(timelines: [Timeline], images: [String: CGImage], imageScale: Double) -> (CGImage?, [String: Double]) {
            var minZIndex = Int.max
            var maxZIndex = Int.min
            var image: CGImage? = nil
            var fades = [String: Double]()
            
            for timeline in timelines {
                if timeline.animation.z < minZIndex {
                    minZIndex = timeline.animation.z
                }
                
                if timeline.animation.z > maxZIndex {
                    maxZIndex = timeline.animation.z
                }
            }
            
            let format = UIGraphicsImageRendererFormat(for: self.traitCollection)
            
            format.opaque = false
            format.preferredRange = .standard
            format.scale = (self.scale == 0.0 ? self.traitCollection.displayScale : self.scale) * imageScale
            
            let renderer = UIGraphicsImageRenderer(size: self.size, format: format)
            let renderedImage = renderer.image { rendererContext in
                let context = rendererContext.cgContext
                let types = self.types.compactMap({ $0.value.1 ? $0.key : nil })
                
                context.interpolationQuality = .high
                context.setAllowsAntialiasing(true)
                context.clear(CGRect(origin: CGPoint.zero, size: self.size))
                context.translateBy(x: 0.0, y: self.size.height)
                context.scaleBy(x: 1.0, y: -1.0)
                
                for z in minZIndex...maxZIndex {
                    for timeline in timelines.reduce(into: [Timeline](), { x, y in
                        if y.animation.z == z && !y.animation.isEmpty && !x.contains(where: { $0.animation.z == y.animation.z && $0.animation.type == y.animation.type }) {
                            x.append(y)
                        }
                    }) {
                        let current = timeline.current
                        
                        if current.opacity > 0.0, let path = current.path {
                            let key: String
                            var tempTypes: [String?]? = nil
                            let isVisible: Bool
                            
                            if timeline.animation.type == nil {
                                key = String(timeline.animation.z)
                                tempTypes = []
                                
                                for cachedTimeline in timelines {
                                    if cachedTimeline.animation.z == timeline.animation.z {
                                        tempTypes!.append(cachedTimeline.animation.type)
                                    }
                                }
                            } else {
                                key = "\(timeline.animation.z)&\(timeline.animation.type!)"
                            }
                            
                            let fade = self.fades[key]
                            
                            if let tempTypes {
                                if types.isEmpty {
                                    isVisible = true
                                } else {
                                    isVisible = !tempTypes.contains { type in
                                        if let type {
                                            return types.contains(type)
                                        }
                                        
                                        return false
                                    }
                                }
                            } else if types.isEmpty {
                                isVisible = false
                            } else {
                                tempTypes = []
                                
                                for cachedTimeline in timelines {
                                    if let type = cachedTimeline.animation.type, cachedTimeline.animation.z == timeline.animation.z && types.contains(type) {
                                        tempTypes!.append(type)
                                    }
                                }
                                
                                isVisible = !tempTypes!.isEmpty && tempTypes!.lastIndex(of: timeline.animation.type!) == tempTypes!.count - 1
                            }
                            
                            if isVisible {
                                if (fade == nil || timeline.animation.type != nil) && timeline.animation.type != nil {
                                    fades[key] = 1.0
                                }
                                
                                if let i = images[path] {
                                    let width: Double
                                    let height: Double
                                    
                                    if current.size.width == 0.0 && current.size.height == 0.0 {
                                        width = floor(Double(i.width) / imageScale)
                                        height = floor(Double(i.height) / imageScale)
                                    } else if current.size.width == 0.0 {
                                        width = floor(current.size.height * Double(i.width) / Double(i.height))
                                        height = floor(current.size.height)
                                    } else if current.size.height == 0.0 {
                                        width = floor(current.size.width)
                                        height = floor(current.size.width * Double(i.height) / Double(i.width))
                                    } else {
                                        width = floor(current.size.width)
                                        height = floor(current.size.height)
                                    }
                                    
                                    context.saveGState()
                                    context.concatenate(CGAffineTransformMakeTranslation(round(self.origin.x + current.location.x), round(self.size.height - self.origin.y - current.location.y - height)))
                                    context.setAlpha(current.opacity)
                                    context.draw(i, in: CGRect(x: 0.0, y: 0.0, width: width, height: height))
                                    context.restoreGState()
                                }
                            } else if fade == nil && timeline.animation.type == nil {
                                fades[key] = 0.0
                            }
                        }
                    }
                }
            }
            
            image = renderedImage.cgImage
            
            return (image, fades)
        }
        
        func audioPlayerDidFinishPlaying(_ player: AVAudioPlayer, successfully flag: Bool) {
            self.audioPlayer = nil
        }
        
        func audioPlayerDecodeErrorDidOccur(_ player: AVAudioPlayer, error: (any Error)?) {
            self.audioPlayer = nil
        }
        
        func locationManagerDidChangeAuthorization(_ manager: CLLocationManager) {
            if manager.authorizationStatus == .notDetermined {
                manager.requestWhenInUseAuthorization()
            }
        }
        
        func locationManager(_ manager: CLLocationManager, didUpdateLocations locations: [CLLocation]) {
            Task {
                if let location = locations.first {
                    let allowedCharacters = NSCharacterSet.alphanumerics.union(.init(charactersIn: "-._~"))
                    
                    if let (data, response) = try? await URLSession.shared.data(for: URLRequest(url: URL(string: "https://milchchan.com/api/weather/\(String(location.coordinate.latitude).addingPercentEncoding(withAllowedCharacters: allowedCharacters)!)/\(String(location.coordinate.longitude).addingPercentEncoding(withAllowedCharacters: allowedCharacters)!)")!)), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "application/json" {
                        
                        if let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [String: Any], let currentWeather = jsonRoot["currentWeather"] as? [String: Any], let conditionCode = currentWeather["conditionCode"] as? String {
                            for character in Script.shared.characters {
                                Task {
                                    await Script.shared.run(name: character.name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                                        if y.name == character.name {
                                            for sequence in y.sequences {
                                                if sequence.name == "Weather" {
                                                    x.append(sequence)
                                                }
                                            }
                                        }
                                    }), state: conditionCode, words: [])
                                }
                            }
                        }
                    }
                }
                
                WidgetCenter.shared.reloadAllTimelines()
                
                if self.refreshControl!.isRefreshing {
                    self.refreshControl!.endRefreshing()
                }
                
                self.parentView?.delegate?.agentDidRefresh(self.parentView!)
            }
        }
        
        func locationManager(_ manager: CLLocationManager, didFailWithError error: Error) {
            WidgetCenter.shared.reloadAllTimelines()
            
            if self.refreshControl!.isRefreshing {
                self.refreshControl!.endRefreshing()
            }
            
            self.parentView?.delegate?.agentDidRefresh(self.parentView!)
        }
        
        private func createBalloonPath(messageWidth: Double,  messageHeight: Double, balloonPartSize: CGSize, radius: Double, n: Double = 2.5) -> CGPath {
            let k = 8.0 * (1.0 / pow(2.0, 1.0 / n) - 1.0 / 2.0) / 3.0
            let balloonPath = CGMutablePath()
            
            balloonPath.move(to: CGPointMake(radius, 0.0))
            balloonPath.addLine(to: CGPointMake(messageWidth - radius, 0.0))
            balloonPath.addCurve(to: CGPointMake(messageWidth, radius), control1: CGPointMake(messageWidth - radius * (1.0 - k), 0.0), control2: CGPointMake(messageWidth, radius * (1.0 - k)))
            balloonPath.addLine(to: CGPointMake(messageWidth, messageHeight - radius))
            balloonPath.addCurve(to: CGPointMake(messageWidth - radius, messageHeight), control1: CGPointMake(messageWidth, messageHeight - radius * (1.0 - k)), control2: CGPointMake(messageWidth - radius * (1.0 - k), messageHeight))
            balloonPath.addLine(to: CGPointMake(messageWidth / 2.0 + balloonPartSize.width / 2.0, messageHeight))
            balloonPath.addLine(to: CGPointMake(messageWidth / 2.0, messageHeight + balloonPartSize.height))
            balloonPath.addLine(to: CGPointMake(messageWidth / 2.0 - balloonPartSize.width / 2.0, messageHeight))
            balloonPath.addLine(to: CGPointMake(radius, messageHeight))
            balloonPath.addCurve(to: CGPointMake(0.0, messageHeight - radius), control1: CGPointMake(radius * (1.0 - k), messageHeight), control2: CGPointMake(0.0, messageHeight - radius * (1.0 - k)))
            balloonPath.addLine(to: CGPointMake(0.0, radius))
            balloonPath.addCurve(to: CGPointMake(radius, 0.0), control1: CGPointMake(0.0, radius * (1.0 - k)), control2: CGPointMake(radius * (1.0 - k), 0.0))
            balloonPath.closeSubpath()
            
            return balloonPath
        }
    }
    
    private actor Upscaler {
        static let shared = Upscaler()
        private(set) var scale = 0.0
        private(set) var isCompleted = false
        private var generation: UInt64 = 0
        
        private init() {}
        
        func run(characters: [(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, upscaling: Bool, language: String?, prompt: String?, guest: Bool, sequences: [Sequence])], completion: (@MainActor @Sendable (Int) -> Void)? = nil) {
            self.isCompleted = false
            self.generation &+= 1
            
            let generation = self.generation
            let sources = characters.reduce(into: [URL: Set<String>]()) { sources, character in
                guard character.upscaling else {
                    return
                }
                
                let baseURL = URL(filePath: character.path).deletingLastPathComponent().standardizedFileURL
                var paths = Set<String>()
                var stack = character.sequences
                
                while let sequence = stack.popLast() {
                    for step in sequence {
                        switch step {
                        case .sequence(let sequence):
                            stack.append(sequence)
                        case .animations(let animations):
                            paths.formUnion(animations.flatMap { $0.compactMap { $0.path }.filter { !$0.isEmpty } })
                        default:
                            break
                        }
                    }
                }
                
                sources[baseURL, default: []].formUnion(paths)
            }
            
            self.removeImages(except: Array(sources.keys))
            
            Task(priority: .utility) { [sources] in
#if !targetEnvironment(simulator)
                if !sources.isEmpty && self.scale == 0 {
                    _ = await self.configure(width: self.minimumTileDimension, height: self.minimumTileDimension, processor: nil)
                }
#endif
                var succeeded = true
                var total = 0

                for (baseURL, paths) in sources {
                    let (isSuccessful, count) = await self.process(paths: paths, baseURL: baseURL)

                    if !isSuccessful {
                        succeeded = false
                    }

                    total += count
                }
                
                if self.generation == generation {
                    self.isCompleted = succeeded

                    await completion?(total)
                }
            }
        }
        
        private nonisolated func readImage(at url: URL) -> CGImage? {
            return autoreleasepool {
                guard FileManager.default.fileExists(atPath: url.path(percentEncoded: false)),
                      let source = CGImageSourceCreateWithURL(url as CFURL, nil) else { return nil }
                return CGImageSourceCreateImageAtIndex(source, 0, [kCGImageSourceShouldCacheImmediately: true] as CFDictionary)
            }
        }
        
        private func removeImages(except baseURLs: [URL]) {
            let names = Set(baseURLs.map(\.lastPathComponent))
            
            guard let documents = try? FileManager.default.url(for: .documentDirectory, in: .userDomainMask, appropriateFor: nil, create: true), let urls = try? FileManager.default.contentsOfDirectory(at: documents, includingPropertiesForKeys: [.isDirectoryKey]) else {
                return
            }
            
            for url in urls where !names.contains(url.lastPathComponent) {
                if (try? url.resourceValues(forKeys: [.isDirectoryKey]))?.isDirectory == true {
                    try? FileManager.default.removeItem(at: url)
                }
            }
        }
        
        private func process(paths: Set<String>, baseURL: URL) async -> (succeeded: Bool, count: Int) {
            guard !paths.isEmpty else {
                return (true, 0)
            }
            
            let directory: URL
            
            do {
                directory = try FileManager.default.url(for: .documentDirectory, in: .userDomainMask, appropriateFor: nil, create: true).appending(path: baseURL.lastPathComponent, directoryHint: .isDirectory)
            } catch {
                return (false, 0)
            }
            
            var succeeded = true
            var imagePaths = Set<String>()
            var directories = [URL: [String]]()
            
            for path in paths {
                guard URL(string: path)?.scheme == nil else {
                    continue
                }
                
                let imageURL = baseURL.appending(path: path, directoryHint: .inferFromPath)
                let name = imageURL.deletingPathExtension().lastPathComponent
                let pathExtension = imageURL.pathExtension
                var candidates = [path]
                
                if name.firstMatch(of: /@[0-9]+x$/) == nil {
                    let sourceDirectory = imageURL.deletingLastPathComponent().standardizedFileURL
                    
                    if directories[sourceDirectory] == nil {
                        if let filenames = try? FileManager.default.contentsOfDirectory(atPath: sourceDirectory.path(percentEncoded: false)) {
                            directories[sourceDirectory] = filenames
                        } else {
                            directories[sourceDirectory] = []
                            succeeded = false
                        }
                    }
                    
                    for filename in directories[sourceDirectory]! {
                        guard (filename as NSString).pathExtension == pathExtension,
                              let match = (filename as NSString).deletingPathExtension.wholeMatch(of: /(.+)@[0-9]+x/),
                              match.output.1 == name else {
                            continue
                        }
                        
                        candidates.append(((path as NSString).deletingLastPathComponent as NSString).appendingPathComponent(filename))
                    }
                }
                
                let existingPaths = candidates.filter {
                    var isDirectory: ObjCBool = false
                    
                    return FileManager.default.fileExists(atPath: baseURL.appending(path: $0, directoryHint: .inferFromPath).path(percentEncoded: false), isDirectory: &isDirectory) && !isDirectory.boolValue
                }
                
                if existingPaths.isEmpty {
                    succeeded = false
                }
                
                imagePaths.formUnion(existingPaths)
            }
            
            var images = imagePaths.compactMap { path -> (path: String, image: CGImage)? in
                let url = directory.appending(path: path, directoryHint: .inferFromPath)
                
                guard self.readImage(at: url) == nil else {
                    return nil
                }
                
                try? FileManager.default.removeItem(at: url)
                
                guard let image = self.readImage(at: baseURL.appending(path: path, directoryHint: .inferFromPath)) else {
                    succeeded = false
                    
                    return nil
                }
                
                return (path, image)
            }
            
            guard !images.isEmpty else {
                return (succeeded, 0)
            }
            
#if !targetEnvironment(simulator)
            guard VTSuperResolutionScalerConfiguration.isSupported else {
                return (false, 0)
            }
            
            images.sort {
                let lhs = self.calculateInputDimensions(for: $0.image)
                let rhs = self.calculateInputDimensions(for: $1.image)
                
                if lhs != rhs {
                    return lhs < rhs
                }
                
                return $0.path < $1.path
            }
            
            let processor = VTFrameProcessor()
            var configuration: VTSuperResolutionScalerConfiguration?
            var total = 0
            
            defer {
                if configuration != nil {
                    processor.endSession()
                }
            }
            
            for (path, original) in images {
                let url = directory.appending(path: path, directoryHint: .inferFromPath)
                
                guard self.readImage(at: url) == nil else {
                    continue
                }
                
                try? FileManager.default.removeItem(at: url)
                
                let image = await self.upscale(original, processor: processor, configuration: &configuration)
                
                guard self.readImage(at: url) == nil else {
                    continue
                }
                
                try? FileManager.default.removeItem(at: url)
                
                guard let image else {
                    succeeded = false
                    
                    if configuration == nil {
                        break
                    }
                    
                    continue
                }
                
                do {
                    try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
                    try autoreleasepool {
                        let data = NSMutableData()
                        
                        guard let destination = CGImageDestinationCreateWithData(data, UTType.png.identifier as CFString, 1, nil) else {
                            throw CocoaError(.fileWriteUnknown)
                        }
                        
                        CGImageDestinationAddImage(destination, image, nil)
                        
                        guard CGImageDestinationFinalize(destination) else {
                            throw CocoaError(.fileWriteUnknown)
                        }
                        
                        try (data as Data).write(to: url, options: .atomic)
                    }

                    total += 1
                } catch {
                    succeeded = false
                }
            }
            
            return (succeeded, total)
#else
            return (false, 0)
#endif
        }
        
#if !targetEnvironment(simulator)
        // https://developer.apple.com/documentation/videotoolbox/vtsuperresolutionscalerconfiguration
        private let padding = 16
        private let minimumTileDimension = 128
        private let maximumTileDimension = 512
        private let colorSpace = CGColorSpace(name: CGColorSpace.sRGB)!
        private lazy var context = CIContext(options: [.cacheIntermediates: false])
        
        private func calculateInputDimensions(for image: CGImage) -> (width: Int, height: Int) {
            func dimension(for length: Int) -> Int {
                let paddedLength = length + self.padding * 2
                var dimension = self.minimumTileDimension
                
                while dimension < paddedLength && dimension < self.maximumTileDimension {
                    dimension *= 2
                }
                
                return dimension
            }
            
            return (dimension(for: image.width), dimension(for: image.height))
        }
        
        private func upscale(_ image: CGImage, processor: VTFrameProcessor, configuration: inout VTSuperResolutionScalerConfiguration?) async -> CGImage? {
            let (width, height) = self.calculateInputDimensions(for: image)
            
            if configuration?.frameWidth != width || configuration?.frameHeight != height {
                if configuration != nil {
                    processor.endSession()
                }
                
                configuration = nil
                configuration = await self.configure(width: width, height: height, processor: processor)
            }
            
            guard let configuration, let pixelFormat = configuration.supportedPixelFormats.first else {
                return nil
            }
            
            let scale = configuration.scaleFactor
            let outputWidth = image.width * scale
            let outputHeight = image.height * scale
            
            guard let output = CGContext(data: nil, width: outputWidth, height: outputHeight, bitsPerComponent: 8, bytesPerRow: outputWidth * 4, space: self.colorSpace, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue | CGBitmapInfo.byteOrder32Big.rawValue), let colors = self.extendTransparentColors(in: image) else {
                return nil
            }
            
            let source = CIImage(cgImage: colors).clampedToExtent()
            let alpha = CIImage(cgImage: image).clampedToExtent().samplingLinear().transformed(by: CGAffineTransform(scaleX: CGFloat(scale), y: CGFloat(scale)))
            let tileWidth = width - self.padding * 2
            let tileHeight = height - self.padding * 2
            
            for y in stride(from: 0, to: image.height, by: tileHeight) {
                for x in stride(from: 0, to: image.width, by: tileWidth) {
                    let contentWidth = min(tileWidth, image.width - x)
                    let contentHeight = min(tileHeight, image.height - y)
                    let input = source.transformed(by: CGAffineTransform(translationX: CGFloat(self.padding - x), y: CGFloat(self.padding - y))).cropped(to: CGRect(x: 0, y: 0, width: width, height: height))
                    
                    guard let sourceBuffer = self.makeBuffer(attributes: configuration.sourcePixelBufferAttributes, pixelFormat: pixelFormat), let destinationBuffer = self.makeBuffer(attributes: configuration.destinationPixelBufferAttributes, pixelFormat: pixelFormat) else {
                        return nil
                    }
                    
                    self.context.render(input, to: sourceBuffer, bounds: input.extent, colorSpace: self.colorSpace)
                    
                    guard let sourceFrame = VTFrameProcessorFrame(buffer: sourceBuffer, presentationTimeStamp: .zero), let destinationFrame = VTFrameProcessorFrame(buffer: destinationBuffer, presentationTimeStamp: .zero), let parameters = VTSuperResolutionScalerParameters(sourceFrame: sourceFrame, previousFrame: nil, previousOutputFrame: nil, opticalFlow: nil, submissionMode: .random, destinationFrame: destinationFrame) else {
                        return nil
                    }
                    
                    let succeeded = await withCheckedContinuation { continuation in
                        processor.process(parameters: parameters) { _, error in
                            continuation.resume(returning: error == nil)
                        }
                    }
                    
                    guard succeeded else {
                        return nil
                    }
                    
                    let destinationRect = CGRect(x: x * scale, y: y * scale, width: contentWidth * scale, height: contentHeight * scale)
                    let result = CIImage(cvPixelBuffer: destinationBuffer, options: [.colorSpace: self.colorSpace]).applyingFilter("CIColorMatrix", parameters: ["inputAVector": CIVector(x: 0, y: 0, z: 0, w: 0), "inputBiasVector": CIVector(x: 0, y: 0, z: 0, w: 1)]).transformed(by: CGAffineTransform(translationX: CGFloat((x - self.padding) * scale), y: CGFloat((y - self.padding) * scale))).cropped(to: destinationRect).applyingFilter("CIBlendWithAlphaMask", parameters: [kCIInputBackgroundImageKey: CIImage(color: .clear).cropped(to: destinationRect), kCIInputMaskImageKey: alpha])
                    
                    guard let tile = self.context.createCGImage(result, from: destinationRect, format: .RGBA8, colorSpace: self.colorSpace) else {
                        return nil
                    }
                    
                    output.draw(tile, in: destinationRect)
                }
            }
            
            return output.makeImage()
        }
        
        private func configure(width: Int, height: Int, processor: VTFrameProcessor?) async -> VTSuperResolutionScalerConfiguration? {
            guard let scale = VTSuperResolutionScalerConfiguration.supportedScaleFactors.filter({ $0 > 1 }).min(), let configuration = VTSuperResolutionScalerConfiguration(frameWidth: width, frameHeight: height, scaleFactor: scale, inputType: .image, usePrecomputedFlow: false, qualityPrioritization: .normal, revision: VTSuperResolutionScalerConfiguration.defaultRevision) else {
                return nil
            }
            
            let scaleFactor = configuration.scaleFactor
            
            if self.scale == 0 {
                self.scale = Double(scaleFactor)
            }
            
            guard let processor else { return configuration }
            
            do {
                if configuration.configurationModelStatus != .ready {
                    try await configuration.downloadConfigurationModel()
                }
                
                try processor.startSession(configuration: configuration)
                
                return configuration
            } catch {
                return nil
            }
        }
        
        private func makeBuffer(attributes: [String: any Sendable], pixelFormat: OSType) -> CVPixelBuffer? {
            let requested: [String: any Sendable] = [kCVPixelBufferPixelFormatTypeKey as String: pixelFormat, kCVPixelBufferIOSurfacePropertiesKey as String: [String: Int]()]
            var resolved: CFDictionary?
            
            guard CVPixelBufferCreateResolvedAttributesDictionary(kCFAllocatorDefault, [attributes, requested] as CFArray, &resolved) == kCVReturnSuccess, let resolved else {
                return nil
            }
            
            var pool: CVPixelBufferPool?
            var buffer: CVPixelBuffer?
            
            guard CVPixelBufferPoolCreate(kCFAllocatorDefault, nil, resolved, &pool) == kCVReturnSuccess, let pool, CVPixelBufferPoolCreatePixelBuffer(kCFAllocatorDefault, pool, &buffer) == kCVReturnSuccess, let buffer else {
                return nil
            }
            
            return buffer
        }
        
        private func extendTransparentColors(in image: CGImage) -> CGImage? {
            let width = image.width
            let height = image.height
            
            guard let context = CGContext(data: nil, width: width, height: height, bitsPerComponent: 8, bytesPerRow: width * 4, space: CGColorSpace(name: CGColorSpace.sRGB)!, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue | CGBitmapInfo.byteOrder32Big.rawValue), let data = context.data else {
                return nil
            }
            
            context.draw(image, in: CGRect(x: 0, y: 0, width: width, height: height))
            
            let bytes = data.assumingMemoryBound(to: UInt8.self)
            var queue = [Int]()
            var visited = [Bool](repeating: false, count: width * height)
            for index in 0..<width * height {
                let offset = index * 4
                let alpha = Int(bytes[offset + 3])
                
                if alpha > 0 {
                    for channel in 0..<3 {
                        bytes[offset + channel] = UInt8(min(255, (Int(bytes[offset + channel]) * 255 + alpha / 2) / alpha))
                    }
                    
                    visited[index] = true
                    queue.append(index)
                }
                
                bytes[offset + 3] = 255
            }
            
            var head = 0
            
            while head < queue.count {
                let index = queue[head]
                
                head += 1
                
                let x = index % width
                let y = index / width
                
                for neighbor in [x > 0 ? index - 1 : -1, x + 1 < width ? index + 1 : -1, y > 0 ? index - width : -1, y + 1 < height ? index + width : -1] where neighbor >= 0 {
                    
                    if !visited[neighbor] {
                        for channel in 0..<3 {
                            bytes[neighbor * 4 + channel] = bytes[index * 4 + channel]
                        }
                        
                        visited[neighbor] = true
                        queue.append(neighbor)
                    }
                }
            }
            
            return context.makeImage()
        }
#endif
    }
}

extension UIWindow {
    open override func motionEnded(_ motion: UIEvent.EventSubtype, with event: UIEvent?) {
        super.motionEnded(motion, with: event)
        
        if motion == .motionShake {
            let safeBounds = self.bounds.inset(by: self.safeAreaInsets)
            
            if safeBounds.width > safeBounds.height {
                for character in Script.shared.characters {
                    Task {
                        await Script.shared.run(name: character.name, sequences: character.sequences.reduce(into: [], { x, y in
                            if y.name == "DoubleClick" || y.name == "DoubleTap" {
                                x.append(y)
                            }
                        }), words: []) { sequences in
                            Script.shared.queue.insert(contentsOf: sequences.reduce(into: [], { x, value in
                                var y = value
                                y.append(.completion)
                                x.append((character.name, y))
                            }), at: 0)
                            
                            return []
                        }
                    }
                }
            } else if let first = Script.shared.characters.first {
                Task {
                    await Script.shared.run(name: first.name, sequences: first.sequences.reduce(into: [], { x, y in
                        if y.name == "DoubleClick" || y.name == "DoubleTap" {
                            x.append(y)
                        }
                    }), words: []) { sequences in
                        Script.shared.queue.insert(contentsOf: sequences.reduce(into: [], { x, value in
                            var y = value
                            y.append(.completion)
                            x.append((first.name, y))
                        }), at: 0)
                        
                        return []
                    }
                }
            }
        }
    }
}

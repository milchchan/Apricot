//
//  Script.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation
import SwiftUI
import CryptoKit

final public class Script: NSObject, ObservableObject {
    public static let shared = Script()
    @Published public var words = [Word]()
    @Published public var attributes = [String]()
    @Published public var scores = [String: (Double, [String]?, String?, Date)]()
    public var characters = [(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, language: String?, prompt: String?, guest: Bool, sequences: [Sequence])]()
    public var likes = [String: [(id: Int?, name: String, content: String, language: String?, attributes: [(name: String?, start: Int, end: Int)], timestamp: Date)]]()
    private let runtime = Script.Runtime()
    public var states: [String: String] {
        get {
            return self.runtime.states
        }
        set {
            self.runtime.states = newValue
        }
    }
    public var queue: [(String, Sequence)] {
        get {
            return self.runtime.queue
        }
        set {
            self.runtime.queue = newValue
        }
    }
    
    private override init() {
        super.init()
        
        if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
            let path = containerUrl.appendingPathComponent("Documents/.words.json").path(percentEncoded: false)
            
            if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                defer {
                    try? file.close()
                }
                
                if let data = try? file.readToEnd() {
                    let decoder = JSONDecoder()
                    
                    if let decoded = try? decoder.decode([Word].self, from: data) {
                        for word in decoded {
                            self.words.append(word)
                        }
                    }
                }
            }
        } else if let url = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {
            let path = url.appendingPathComponent("words.json").path(percentEncoded: false)
            
            if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                defer {
                    try? file.close()
                }
                
                if let data = try? file.readToEnd() {
                    let decoder = JSONDecoder()
                    
                    if let decoded = try? decoder.decode([Word].self, from: data) {
                        for word in decoded {
                            self.words.append(word)
                        }
                    }
                }
            }
        }
        
        if let url = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first {
            let path = url.appending(path: "likes.json", directoryHint: .inferFromPath).path(percentEncoded: false)
            
            if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                defer {
                    try? file.close()
                }
                
                if let data = try? file.readToEnd(), let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [Any] {
                    for obj in jsonRoot {
                        if let likeObject = obj as? [String: Any?] {
                            var name: String? = nil
                            var content: String? = nil
                            var language: String? = nil
                            var attributes = [(name: String?, start: Int, end: Int)]()
                            var date: Date? = nil
                            
                            if let value = likeObject["name"] as? String {
                                name = value
                            }
                            
                            if let value = likeObject["content"] as? String {
                                content = value
                            }
                            
                            if let value = likeObject["language"] as? String {
                                language = value
                            }
                            
                            if let attributeArray = likeObject["attributes"] as? [[String: Any]] {
                                for dictionaryObject in attributeArray {
                                    var attributeStart: Int? = nil
                                    var attributeEnd: Int? = nil
                                    
                                    if let value = dictionaryObject["start"] as? Double {
                                        attributeStart = Int(value)
                                    }
                                    
                                    if let value = dictionaryObject["end"] as? Double {
                                        attributeEnd = Int(value)
                                    }
                                    
                                    if let attributeStart, let attributeEnd {
                                        attributes.append((name: dictionaryObject["name"] as? String, start: attributeStart, end: attributeEnd))
                                    }
                                }
                            }
                            
                            if let value = likeObject["timestamp"] as? Double {
                                date = Date(timeIntervalSince1970: value)
                            }
                            
                            if let name, let content, let language, let date {
                                if var values = self.likes[name] {
                                    values.append((id: nil, name: name, content: content, language: language, attributes: attributes, timestamp: date))
                                    self.likes[name] = values
                                } else {
                                    self.likes[name] = [(id: nil, name: name, content: content, language: language, attributes: attributes, timestamp: date)]
                                }
                            }
                        }
                    }
                }
            }
        }
        
        if let cachesUrl = FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask).first {
            let path = cachesUrl.appendingPathComponent(SHA256.hash(data: Data(URL(string: "https://milchchan.com/api/likes")!.absoluteString.utf8)).compactMap { String(format: "%02x", $0) }.joined()).path(percentEncoded: false)
            
            if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                defer {
                    try? file.close()
                }
                
                if let data = try? file.readToEnd(), let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [Any] {
                    var words = [Word]()
                    var documents = [[String]]()
                    var metadata = [([[String]?], String?, Date)]()
                    
                    for word in self.words {
                        if word.attributes == nil || !word.attributes!.isEmpty {
                            words.append(word)
                        }
                    }
                    
                    for obj in jsonRoot {
                        if let likeObject = obj as? [String: Any?] {
                            var id: Int? = nil
                            var name: String? = nil
                            var content: String? = nil
                            var attributes = [(name: String?, start: Int, end: Int)]()
                            var language: String? = nil
                            var date: Date? = nil
                            
                            if let value = likeObject["id"] as? Int {
                                id = value
                            }
                            
                            if let value = likeObject["name"] as? String {
                                name = value
                            }
                            
                            if let value = likeObject["content"] as? String {
                                content = value
                            }
                            
                            if let value = likeObject["language"] as? String {
                                language = value
                            }
                            
                            if let attributeArray = likeObject["attributes"] as? [[String: Any]] {
                                for dictionaryObject in attributeArray {
                                    var attributeStart: Int? = nil
                                    var attributeEnd: Int? = nil
                                    
                                    if let value = dictionaryObject["start"] as? Double {
                                        attributeStart = Int(value)
                                    }
                                    
                                    if let value = dictionaryObject["end"] as? Double {
                                        attributeEnd = Int(value)
                                    }
                                    
                                    if let attributeStart, let attributeEnd {
                                        attributes.append((name: dictionaryObject["name"] as? String, start: attributeStart, end: attributeEnd))
                                    }
                                }
                            }
                            
                            if let value = likeObject["timestamp"] as? Double {
                                date = Date(timeIntervalSince1970: value)
                            }
                            
                            if let id, let name, let content, let language, let date {
                                if var values = self.likes[name] {
                                    if !values.contains(where: { like in
                                        if like.id == nil && like.content == content && like.attributes.count == attributes.count {
                                            for i in 0..<attributes.count {
                                                if like.attributes[i].start != attributes[i].start || like.attributes[i].end != attributes[i].end {
                                                    return false
                                                }
                                            }
                                            
                                            return true
                                        }
                                        
                                        return false
                                    }) {
                                        values.append((id: id, name: name, content: content, language: language, attributes: attributes, timestamp: date))
                                        self.likes[name] = values
                                    }
                                } else {
                                    self.likes[name] = [(id: id, name: name, content: content, language: language, attributes: attributes, timestamp: date)]
                                }
                            }
                        }
                    }
                    
                    for likes in self.likes.values {
                        for like in likes {
                            if !like.attributes.isEmpty {
                                var index = 0
                                var document = [String]()
                                var annotations = [[String]?]()
                                
                                while index < like.content.count {
                                    var maxEnd = index
                                    var boundaryIndex = index
                                    
                                    for attribute in like.attributes {
                                        if attribute.start == index && attribute.end > maxEnd {
                                            boundaryIndex = maxEnd
                                            maxEnd = attribute.end
                                        }
                                    }
                                    
                                    if index != maxEnd {
                                        var attributes = [String]()
                                        
                                        for attribute in like.attributes {
                                            if attribute.end == maxEnd, let name = attribute.name, !attributes.contains(name) {
                                                attributes.append(name)
                                            }
                                        }
                                        
                                        if boundaryIndex == index {
                                            document.append(String(like.content[like.content.index(like.content.startIndex, offsetBy: index)..<like.content.index(like.content.startIndex, offsetBy: maxEnd)]))
                                            
                                            if attributes.isEmpty {
                                                annotations.append(nil)
                                            } else {
                                                annotations.append(attributes)
                                            }
                                        } else {
                                            document.append(String(like.content[like.content.index(like.content.startIndex, offsetBy: index)..<like.content.index(like.content.startIndex, offsetBy: boundaryIndex)]).trimmingCharacters(in: .whitespaces))
                                            annotations.append([])
                                            document.append(String(like.content[like.content.index(like.content.startIndex, offsetBy: boundaryIndex)..<like.content.index(like.content.startIndex, offsetBy: maxEnd)]))
                                            
                                            if attributes.isEmpty {
                                                annotations.append(nil)
                                            } else {
                                                annotations.append(attributes)
                                            }
                                        }
                                        
                                        index = maxEnd
                                        
                                        continue
                                    }
                                    
                                    index += 1
                                }
                                
                                documents.append(document)
                                metadata.append((annotations, like.language, like.timestamp))
                            }
                        }
                    }
                    
                    if !documents.isEmpty {
                        let scores = self.computeBM25(documents: documents)
                        let yesterday = Date(timeIntervalSinceNow: -60 * 60 * 24)
                        var recentLikes = [([String: (Double, [String]?)], String?, Date)]()
                        
                        for i in 0..<documents.count {
                            if metadata[i].2 > yesterday {
                                var temp = [String: (Double, [String]?)]()
                                
                                for (key, value) in scores[i] {
                                    for j in 0..<documents[i].count {
                                        if key == documents[i][j] {
                                            temp[key] = (value, metadata[i].0[j])
                                            
                                            break
                                        }
                                    }
                                }
                                
                                recentLikes.append((temp, metadata[i].1, metadata[i].2))
                            }
                        }
                        
                        if recentLikes.isEmpty {
                            for i in 0..<documents.count {
                                var temp = [String: (Double, [String]?)]()
                                
                                for (key, value) in scores[i] {
                                    for j in 0..<documents[i].count {
                                        if key == documents[i][j] {
                                            temp[key] = (value, metadata[i].0[j])
                                            
                                            break
                                        }
                                    }
                                }
                                
                                recentLikes.append((temp, metadata[i].1, metadata[i].2))
                            }
                            
                            recentLikes.sort { $0.2 > $1.2 }
                            recentLikes = Array(recentLikes[0..<max(Int(round(Double(documents.count) * 0.1)), 1)])
                        }
                        
                        for like in recentLikes {
                            for (key, value) in like.0 {
                                if let tuple = self.scores[key] {
                                    if var currentAttributes = tuple.1 {
                                        if let attributes = value.1 {
                                            for attribute in attributes {
                                                if !currentAttributes.contains(attribute) {
                                                    currentAttributes.append(attribute)
                                                }
                                            }
                                        }
                                        
                                        self.scores[key] = (value.0 + tuple.0, currentAttributes, like.1, tuple.3)
                                    } else {
                                        self.scores[key] = (value.0 + tuple.0, value.1, like.1, tuple.3)
                                    }
                                } else {
                                    self.scores[key] = (value.0, value.1, like.1, like.2)
                                }
                            }
                        }
                        
                        for (key, value) in self.scores {
                            self.scores[key] = (value.0 / Double(recentLikes.count), value.1, value.2, value.3)
                        }
                    }
                }
            }
        }
    }
    
    public static func resolve(directory: String) -> [String] {
        var paths = [String: [(String, String?)]]()
        let parser = Parser()
        
        parser.excludeSequences = true
        
        if let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
            if let urls = try? FileManager.default.contentsOfDirectory(at: containerUrl.appending(path: directory, directoryHint: .isDirectory), includingPropertiesForKeys: [.nameKey], options: .skipsHiddenFiles) {
                for url in urls {
                    if let values = try? url.resourceValues(forKeys: [.nameKey]), let name = values.name, let match = name.wholeMatch(of: /^(.+?)(?:\.([a-z]{2,3}))?\.(?:json|xml)$/) {
                        let key = String(match.output.1)
                        let path = url.path(percentEncoded: false)
                        
                        if var tuple = paths[key] {
                            if let output = match.output.2 {
                                var languageCode = String(output)
                                
                                for character in parser.parse(path: path).0 {
                                    if let language = character.language {
                                        languageCode = language
                                    }
                                }
                                
                                tuple.append((path, String(languageCode)))
                            } else {
                                tuple.append((path, nil))
                            }
                            
                            paths[key] = tuple
                        } else if let output = match.output.2 {
                            var languageCode = String(output)
                            
                            for character in parser.parse(path: path).0 {
                                if let language = character.language {
                                    languageCode = language
                                }
                            }
                            
                            paths[key] = [(path, String(languageCode))]
                        } else {
                            paths[key] = [(path, nil)]
                        }
                    }
                }
            }
        }
        
        if paths.isEmpty {
            for path in Bundle.main.paths(forResourcesOfType: "xml", inDirectory: directory) {
                let input = URL(filePath: path).deletingPathExtension().lastPathComponent
                
                if let match = input.wholeMatch(of: /^(.+?)\.([a-z]{2,3})$/) {
                    let key = String(match.output.1)
                    var languageCode = String(match.output.2)
                    
                    for character in parser.parse(path: path).0 {
                        if let language = character.language {
                            languageCode = language
                        }
                    }
                    
                    if var tuple = paths[key] {
                        tuple.append((path, languageCode))
                        paths[key] = tuple
                    } else {
                        paths[key] = [(path, languageCode)]
                    }
                } else if var tuple = paths[input] {
                    tuple.append((path, nil))
                    paths[input] = tuple
                } else {
                    paths[input] = [(path, nil)]
                }
            }
        }
        
        if let preferredLanguage = Locale.preferredLanguages.first {
            if let languageCode = Locale(identifier: preferredLanguage).language.languageCode {
                var tempPaths = [String]()
                
                for value in paths.values {
                    for tuple in value {
                        if tuple.1 == languageCode.identifier {
                            tempPaths.append(tuple.0)
                        }
                    }
                }
                
                if !tempPaths.isEmpty {
                    return tempPaths
                }
            }
        }
        
        return paths.values.reduce(into: [], { x, y in
            for tuple in y {
                if tuple.1 == nil {
                    x.append(tuple.0)
                }
            }
        })
    }
    
    public func update() async -> Bool {
        let url = URL(string: "https://milchchan.com/api/likes")!
        var json: [Any] = []
        
        json.append(contentsOf: await Task.detached {
            var json: [Any] = []
            
            if let cachesUrl = FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask).first {
                let hash = SHA256.hash(data: Data(url.absoluteString.utf8)).compactMap { String(format: "%02x", $0) }.joined()
                
                if let urls = try? FileManager.default.contentsOfDirectory(at: cachesUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
                    for url in urls {
                        if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), let isDirectory = values.isDirectory, !isDirectory, let name = values.name, name == hash {
                            let path = url.path(percentEncoded: false)
                            
                            if let file = FileHandle(forReadingAtPath: path) {
                                defer {
                                    try? file.close()
                                }
                                
                                if let data = try? file.readToEnd(), let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [Any] {
                                    json.append(contentsOf: jsonRoot)
                                }
                            }
                            
                            break
                        }
                    }
                }
            }
            
            return json
        }.value)
        
        if let (data, response) = try? await URLSession.shared.data(for: URLRequest(url: url)), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "application/json" {
            json.append(contentsOf: await Task.detached {
                var json: [Any] = []
                
                if let cachesUrl = FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask).first {
                    let path = cachesUrl.appending(path: SHA256.hash(data: Data(url.absoluteString.utf8)).compactMap { String(format: "%02x", $0) }.joined(), directoryHint: .inferFromPath).path(percentEncoded: false)
                    
                    if FileManager.default.fileExists(atPath: path) {
                        if let file = FileHandle(forWritingAtPath: path) {
                            defer {
                                try? file.close()
                            }
                            
                            try? file.truncate(atOffset: 0)
                            try? file.write(contentsOf: data)
                        }
                    } else {
                        FileManager.default.createFile(atPath: path, contents: data, attributes: nil)
                    }
                }
                
                if let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [Any] {
                    json.append(contentsOf: jsonRoot)
                }
                
                return json
            }.value)
        }
        
        let tempJson = json
        let data = await Task.detached {
            var likes = [String: [(id: Int?, name: String, content: String, language: String?, attributes: [(name: String?, start: Int, end: Int)], timestamp: Date)]]()
            var tempLikes = [String: [(id: Int?, name: String, content: String, language: String?, attributes: [(name: String?, start: Int, end: Int)], timestamp: Date)]]()
            var idSet = Set<Int>()
            var words = [Word]()
            var isUpdated = false
            var documents = [[String]]()
            var metadata = [([[String]?], String?, Date)]()
            var data = [String: (Double, [String]?, String?, Date)]()
            
            for (key, value) in self.likes {
                for like in value {
                    if let id = like.id {
                        idSet.insert(id)
                    } else {
                        if var value = likes[key] {
                            value.append(like)
                            likes[key] = value
                            tempLikes[key] = value
                        } else {
                            likes[key] = [like]
                            tempLikes[key] = [like]
                        }
                    }
                }
            }
            
            for word in self.words {
                if word.attributes == nil || !word.attributes!.isEmpty {
                    words.append(word)
                }
            }
            
            for obj in tempJson {
                if let likeObject = obj as? [String: Any?] {
                    var id: Int? = nil
                    var name: String? = nil
                    var content: String? = nil
                    var language: String? = nil
                    var attributes = [(name: String?, start: Int, end: Int)]()
                    var date: Date? = nil
                    
                    if let value = likeObject["id"] as? Int {
                        id = value
                    }
                    
                    if let value = likeObject["name"] as? String {
                        name = value
                    }
                    
                    if let value = likeObject["content"] as? String {
                        content = value
                    }
                    
                    if let value = likeObject["language"] as? String {
                        language = value
                    }
                    
                    if let attributeArray = likeObject["attributes"] as? [[String: Any]] {
                        for dictionaryObject in attributeArray {
                            var attributeStart: Int? = nil
                            var attributeEnd: Int? = nil
                            
                            if let value = dictionaryObject["start"] as? Double {
                                attributeStart = Int(value)
                            }
                            
                            if let value = dictionaryObject["end"] as? Double {
                                attributeEnd = Int(value)
                            }
                            
                            if let attributeStart, let attributeEnd {
                                attributes.append((name: dictionaryObject["name"] as? String, start: attributeStart, end: attributeEnd))
                            }
                        }
                    }
                    
                    if let value = likeObject["timestamp"] as? Double {
                        date = Date(timeIntervalSince1970: value)
                    }
                    
                    if let id, let name, let content, let language, let date {
                        if var values = tempLikes[name] {
                            if !values.contains(where: { like in
                                if like.id == nil {
                                    if like.content == content && like.attributes.count == attributes.count {
                                        for i in 0..<attributes.count {
                                            if like.attributes[i].start != attributes[i].start || like.attributes[i].end != attributes[i].end {
                                                return false
                                            }
                                        }
                                        
                                        return true
                                    }
                                }
                                
                                return like.id == id
                            }) {
                                values.append((id: id, name: name, content: content, language: language, attributes: attributes, timestamp: date))
                                tempLikes[name] = values
                                
                                if !idSet.contains(id) && Script.shared.characters.contains(where: { $0.language == language }) {
                                    isUpdated = true
                                }
                            }
                        } else {
                            tempLikes[name] = [(id: id, name: name, content: content, language: language, attributes: attributes, timestamp: date)]
                            
                            if !idSet.contains(id) && Script.shared.characters.contains(where: { $0.language == language }) {
                                isUpdated = true
                            }
                        }
                    }
                }
            }
            
            for value in tempLikes.values {
                for like in value {
                    if !like.attributes.isEmpty {
                        var index = 0
                        var document = [String]()
                        var annotations = [[String]?]()
                        
                        while index < like.content.count {
                            var maxEnd = index
                            var boundaryIndex = index
                            
                            for attribute in like.attributes {
                                if attribute.start == index && attribute.end > maxEnd {
                                    boundaryIndex = maxEnd
                                    maxEnd = attribute.end
                                }
                            }
                            
                            if index != maxEnd {
                                var attributes = [String]()
                                
                                for attribute in like.attributes {
                                    if attribute.end == maxEnd, let name = attribute.name, !attributes.contains(name) {
                                        attributes.append(name)
                                    }
                                }
                                
                                if boundaryIndex == index {
                                    document.append(String(like.content[like.content.index(like.content.startIndex, offsetBy: index)..<like.content.index(like.content.startIndex, offsetBy: maxEnd)]))
                                    
                                    if attributes.isEmpty {
                                        annotations.append(nil)
                                    } else {
                                        annotations.append(attributes)
                                    }
                                } else {
                                    document.append(String(like.content[like.content.index(like.content.startIndex, offsetBy: index)..<like.content.index(like.content.startIndex, offsetBy: boundaryIndex)]).trimmingCharacters(in: .whitespaces))
                                    annotations.append([])
                                    document.append(String(like.content[like.content.index(like.content.startIndex, offsetBy: boundaryIndex)..<like.content.index(like.content.startIndex, offsetBy: maxEnd)]))
                                    
                                    if attributes.isEmpty {
                                        annotations.append(nil)
                                    } else {
                                        annotations.append(attributes)
                                    }
                                }
                                
                                index = maxEnd
                                
                                continue
                            }
                            
                            index += 1
                        }
                        
                        documents.append(document)
                        metadata.append((annotations, like.language, like.timestamp))
                    }
                }
            }
            
            if !documents.isEmpty {
                let scores = self.computeBM25(documents: documents)
                let yesterday = Date(timeIntervalSinceNow: -60 * 60 * 24)
                var recentLikes = [([String: (Double, [String]?)], String?, Date)]()
                
                for i in 0..<documents.count {
                    if metadata[i].2 > yesterday {
                        var temp = [String: (Double, [String]?)]()
                        
                        for (key, value) in scores[i] {
                            for j in 0..<documents[i].count {
                                if key == documents[i][j] {
                                    temp[key] = (value, metadata[i].0[j])
                                    
                                    break
                                }
                            }
                        }
                        
                        recentLikes.append((temp, metadata[i].1, metadata[i].2))
                    }
                }
                
                if recentLikes.isEmpty {
                    for i in 0..<documents.count {
                        var temp = [String: (Double, [String]?)]()
                        
                        for (key, value) in scores[i] {
                            for j in 0..<documents[i].count {
                                if key == documents[i][j] {
                                    temp[key] = (value, metadata[i].0[j])
                                    
                                    break
                                }
                            }
                        }
                        
                        recentLikes.append((temp, metadata[i].1, metadata[i].2))
                    }
                    
                    recentLikes.sort { $0.2 > $1.2 }
                }
                
                for like in recentLikes {
                    for (key, value) in like.0 {
                        if let tuple = data[key] {
                            if var currentAttributes = tuple.1 {
                                if let attributes = value.1 {
                                    for attribute in attributes {
                                        if !currentAttributes.contains(attribute) {
                                            currentAttributes.append(attribute)
                                        }
                                    }
                                }
                                
                                data[key] = (value.0 + tuple.0, currentAttributes, like.1, tuple.3)
                            } else {
                                data[key] = (value.0 + tuple.0, value.1, like.1, tuple.3)
                            }
                        } else {
                            data[key] = (value.0, value.1, like.1, like.2)
                        }
                    }
                }
                
                for (key, value) in data {
                    data[key] = (value.0 / Double(recentLikes.count), value.1, value.2, value.3)
                }
            }
            
            return (isUpdated, tempLikes.reduce(into: [String: [(id: Int?, name: String, content: String, language: String?, attributes: [(name: String?, start: Int, end: Int)], timestamp: Date)]](), { x, y in
                for like in y.value {
                    if like.id != nil {
                        if var value = x[y.key] {
                            value.append(like)
                            x[y.key] = value
                        } else {
                            x[y.key] = [like]
                        }
                    }
                }
            }), data)
        }.value
        
        await MainActor.run { [weak self] in
            guard var likes = self?.likes, let scores = self?.scores else {
                return
            }
            
            for (key, value) in likes {
                for i in stride(from: value.count - 1, through: 0, by: -1) {
                    if value[i].id != nil {
                        likes[key]!.remove(at: i)
                        self?.likes[key]!.remove(at: i)
                    }
                }
            }
            
            for (_, value) in data.1 {
                for like in value {
                    if var values = likes[like.name] {
                        if !values.contains(where: { x in
                            if x.id == nil && x.content == like.content && x.attributes.count == like.attributes.count {
                                for i in 0..<like.attributes.count {
                                    if x.attributes[i].start != like.attributes[i].start || x.attributes[i].end != like.attributes[i].end {
                                        return false
                                    }
                                }
                                
                                return true
                            }
                            
                            return false
                        }) {
                            values.append(like)
                            likes[like.name] = values
                            self?.likes[like.name] = values
                        }
                    } else {
                        likes[like.name] = [like]
                        self?.likes[like.name] = [like]
                    }
                }
            }
            
            for (key, value) in likes {
                if value.isEmpty {
                    self?.likes.removeValue(forKey: key)
                }
            }
            
            for (key, value) in data.2 {
                self?.scores[key] = value
            }
            
            for (key, _) in scores {
                if data.2[key] == nil {
                    self?.scores.removeValue(forKey: key)
                }
            }
        }
        
        return data.0
    }
    
    public func run(name: String, sequences: [Sequence], state: String? = nil, completion: (([Sequence]) -> [Sequence])? = nil) {
        self.runtime.run(characters: self.characters, name: name, sequences: sequences, state: state, completion: completion)
    }
    
    public func run(name: String, sequences: [Sequence], state: String? = nil, words: [Word], temperature: Double = 1.0, completion: (([Sequence]) -> [Sequence])? = nil) async {
        await self.runtime.run(characters: self.characters, name: name, sequences: sequences, state: state, scores: self.scores, words: words, temperature: temperature, completion: completion)
    }
    
    private func computeBM25(documents: [[String]], k1: Double = 1.2, b: Double = 0.75) -> [[String: Double]] {
        // Okapi BM25
        // Stephen E. Robertson, Steve Walker, Susan Jones, Micheline Hancock-Beaulieu, and Mike Gatford. Okapi at TREC-3. In Proceedings of the Third Text REtrieval Conference (TREC 1994). Gaithersburg, USA, November 1994.
        var bags = [[String: Int]]()
        var tokenSet = Set<String>()
        var idf = [String: Double]() // Inverse document frequency
        var avgdl = 0.0 // Average document length
        var d = [(tf: [String: Double], dl: Double)]()
        var bm25 = [[String: Double]]()
        
        for document in documents {
            var bow = [String: Int]() // Bag-of-words
            
            for token in document {
                if let count = bow[token] {
                    bow[token] = count + 1
                } else {
                    bow[token] = 1
                }
                
                if !tokenSet.contains(token) {
                    tokenSet.insert(token)
                }
            }
            
            bags.append(bow)
        }
        
        for bag in bags {
            var tf = [String: Double]() // Term frequency
            var dl = 0.0 // Document length
            
            for (key, value) in bag {
                dl += Double(value)
                
                if let count = idf[key] {
                    if value > 0 {
                        idf[key] = count + 1.0
                    }
                } else if value > 0 {
                    idf[key] = 1.0
                } else {
                    idf[key] = 0.0
                }
            }
            
            for (key, value) in bag {
                tf[key] = Double(value) / dl
            }
            
            d.append((tf: tf, dl: dl))
            avgdl += dl
        }
        
        for (key, value) in idf {
            idf[key] = logl(1.0 + (Double(bags.count) - value + 0.5) / (value + 0.5))
        }
        
        avgdl /= Double(bags.count)
        
        for (tf, dl) in d {
            var scores = [String: Double]()
            
            for token in tokenSet {
                if let f = tf[token] {
                    let numerator = f * (k1 + 1.0)
                    let denominator = f + k1 * (1.0 - b + b * (dl / avgdl))
                    
                    scores[token] = idf[token]! * (numerator / denominator)
                }
            }
            
            bm25.append(scores)
        }
        
        return bm25
    }
    
    public class Runtime: NSObject {
        public var states = [String: String]()
        public var queue = [(String, Sequence)]()
        private let innerSemaphore = Semaphore(value: 1)
        private let outerSemaphore = Semaphore(value: 1)
        
        public func run(characters: [(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, language: String?, prompt: String?, guest: Bool, sequences: [Sequence])], name: String, sequences: [Sequence], state: String? = nil, completion: (([Sequence]) -> [Sequence])? = nil) {
            var preparedSequences = self.prepare(characters: characters, name: name, sequences: sequences, state: state, transform: { (s) -> [Sequence] in
                return s.isEmpty ? s : [s[Int.random(in: 0..<s.count)]]
            })
            
            if let completion {
                preparedSequences = completion(preparedSequences)
            }
            
            for sequence in preparedSequences {
                sequence.append(nil)
                self.queue.append((name, sequence))
            }
        }
        
        public func run(characters: [(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, language: String?, prompt: String?, guest: Bool, sequences: [Sequence])], name: String, sequences: [Sequence], state: String? = nil, scores: [String: (Double, [String]?, String?, Date)], words: [Word], temperature: Double = 1.0, completion: (([Sequence]) -> [Sequence])? = nil) async {
            var preparedSequences = await self.prepareAsync(characters: characters, name: name, sequences: sequences, state: state, scores: scores, words: words, temperature: temperature)
            
            if let completion {
                preparedSequences = completion(preparedSequences)
            }
            
            for sequence in preparedSequences {
                sequence.append(nil)
                self.queue.append((name, sequence))
            }
        }
        
        private func prepare(characters: [(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, language: String?, prompt: String?, guest: Bool, sequences: [Sequence])], name: String, sequences: [Sequence], state: String? = nil, transform: (([Sequence]) -> [Sequence])? = nil) -> [Sequence] {
            var executionQueue: [(sequences: [Sequence], state: String?, next: (sequence: Sequence, objects: [Any?], sequences: [Sequence])?)] = [(sequences: sequences, state: state, next: nil)]
            var preparedSequences = [Sequence]()
            
            repeat {
                var (currentSequences, currentState, next) = executionQueue.removeFirst()
                var selectedSequences = [Sequence]()
                var tempPreparedSequences = [Sequence]()
                
                for sequence in currentSequences {
                    if let pattern = sequence.state {
                        if let currentState {
                            if currentState.isEmpty {
                                if let name = sequence.name, self.states.keys.contains(name) {
                                    self.states.removeValue(forKey: name)
                                }
                            } else if let regex = try? Regex(pattern), let match = currentState.firstMatch(of: regex), !match.output.isEmpty {
                                selectedSequences.append(sequence)
                            }
                        } else if let name = sequence.name, let input = self.states[name], let regex = try? Regex(pattern), let match = input.firstMatch(of: regex), !match.output.isEmpty {
                            selectedSequences.append(sequence)
                        }
                    }
                }
                
                if selectedSequences.isEmpty {
                    for sequence in currentSequences {
                        if sequence.state == nil {
                            selectedSequences.append(sequence)
                        }
                    }
                    
                    currentState = nil
                } else if currentState != nil && currentState!.isEmpty {
                    currentState = nil
                }
                
                if !selectedSequences.isEmpty {
                    if let transform {
                        selectedSequences = transform(selectedSequences)
                    }
                    
                    for i in 0..<selectedSequences.count {
                        let selectedSequence = selectedSequences[i]
                        var isAborted = false
                        let flattenedSequence = Sequence(name: selectedSequence.name, state: selectedSequence.state)
                        
                        if let name = selectedSequence.name, let currentState {
                            self.states[name] = currentState
                        }
                        
                        for j in 0..<selectedSequence.count {
                            let obj = selectedSequence[j]
                            
                            if let nestedSequence = obj as? Sequence {
                                if nestedSequence.isEmpty {
                                    var callableSequences = [Sequence]()
                                    var nextSequence: Sequence
                                    var nextObjects: [Any?]
                                    var nextSequences: [Sequence]
                                    
                                    for character in characters {
                                        if name == character.name {
                                            for sourceSequence in character.sequences {
                                                var sequenceStack = self.getSequenceStack(sourceSequence: sourceSequence, targetSequence: nestedSequence)
                                                
                                                if sequenceStack.isEmpty {
                                                    callableSequences.append(sourceSequence)
                                                } else {
                                                    var stack = [Sequence]()
                                                    
                                                    repeat {
                                                        let poppedSequence = sequenceStack.popLast()!
                                                        
                                                        if !sequenceStack.isEmpty {
                                                            var tempStack = [Sequence]()
                                                            
                                                            for sequence in sequenceStack.last! {
                                                                if let s = sequence as? Sequence {
                                                                    if !s.isEmpty && !stack.contains(where: { $0 === s }) {
                                                                        tempStack.append(s)
                                                                    }
                                                                    
                                                                    if s === poppedSequence {
                                                                        break
                                                                    }
                                                                }
                                                            }
                                                            
                                                            while !tempStack.isEmpty {
                                                                stack.append(tempStack.popLast()!)
                                                            }
                                                        }
                                                        
                                                        if !poppedSequence.isEmpty && !stack.contains(where: { $0 === poppedSequence }) {
                                                            stack.append(poppedSequence)
                                                        }
                                                    } while !sequenceStack.isEmpty
                                                    
                                                    while !stack.isEmpty {
                                                        callableSequences.append(stack.popLast()!)
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    
                                    if let (sequence, objects, sequences) = next {
                                        nextSequence = sequence
                                        
                                        for o in flattenedSequence {
                                            nextSequence.append(o)
                                        }
                                        
                                        nextObjects = objects
                                        nextSequences = sequences
                                    } else {
                                        nextSequence = flattenedSequence
                                        nextObjects = []
                                        nextSequences = []
                                    }
                                    
                                    for k in j + 1..<selectedSequence.count {
                                        nextObjects.append(selectedSequence[k])
                                    }
                                    
                                    for k in i + 1..<selectedSequences.count {
                                        nextSequences.append(selectedSequences[k])
                                    }
                                    
                                    executionQueue.append((sequences: callableSequences.filter { $0.name == nestedSequence.name }, state: nestedSequence.state, next: (sequence: nextSequence, objects: nextObjects, sequences: nextSequences)))
                                    isAborted = true
                                    
                                    break
                                }
                            } else {
                                flattenedSequence.append(obj)
                            }
                        }
                        
                        if isAborted {
                            tempPreparedSequences.removeAll()
                            next = nil
                            
                            break
                        }
                        
                        if !flattenedSequence.isEmpty {
                            tempPreparedSequences.append(flattenedSequence)
                        }
                    }
                }
                
                if let (flattenedSequence, objects, sequences) = next {
                    var isAborted = false
                    
                    for s in tempPreparedSequences {
                        for o in s {
                            flattenedSequence.append(o)
                        }
                    }
                    
                    for i in 0..<objects.count {
                        let obj = objects[i]
                        
                        if let nestedSequence = obj as? Sequence {
                            if nestedSequence.isEmpty {
                                var callableSequences = [Sequence]()
                                var nextObjects = [Any?]()
                                
                                for character in characters {
                                    if name == character.name {
                                        for sourceSequence in character.sequences {
                                            var sequenceStack = self.getSequenceStack(sourceSequence: sourceSequence, targetSequence: nestedSequence)
                                            
                                            if sequenceStack.isEmpty {
                                                callableSequences.append(sourceSequence)
                                            } else {
                                                var stack = [Sequence]()
                                                
                                                repeat {
                                                    let poppedSequence = sequenceStack.popLast()!
                                                    
                                                    if !sequenceStack.isEmpty {
                                                        var tempStack = [Sequence]()
                                                        
                                                        for sequence in sequenceStack.last! {
                                                            if let s = sequence as? Sequence {
                                                                if !s.isEmpty && !stack.contains(where: { $0 === s }) {
                                                                    tempStack.append(s)
                                                                }
                                                                
                                                                if s === poppedSequence {
                                                                    break
                                                                }
                                                            }
                                                        }
                                                        
                                                        while !tempStack.isEmpty {
                                                            stack.append(tempStack.popLast()!)
                                                        }
                                                    }
                                                    
                                                    if !poppedSequence.isEmpty && !stack.contains(where: { $0 === poppedSequence }) {
                                                        stack.append(poppedSequence)
                                                    }
                                                } while !sequenceStack.isEmpty
                                                
                                                while !stack.isEmpty {
                                                    callableSequences.append(stack.popLast()!)
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                for j in i + 1..<objects.count {
                                    nextObjects.append(objects[j])
                                }
                                
                                executionQueue.append((sequences: callableSequences.filter { $0.name == nestedSequence.name }, state: nestedSequence.state, next: (sequence: flattenedSequence, objects: nextObjects, sequences: sequences)))
                                isAborted = true
                                
                                break
                            }
                        } else {
                            flattenedSequence.append(obj)
                        }
                    }
                    
                    if isAborted {
                        continue
                    }
                    
                    if !flattenedSequence.isEmpty {
                        preparedSequences.append(flattenedSequence)
                    }
                    
                    for i in 0..<sequences.count {
                        let selectedSequence = sequences[i]
                        let tempFlattenedSequence = Sequence(name: selectedSequence.name, state: selectedSequence.state)
                        
                        if let name = selectedSequence.name, let s = currentState {
                            self.states[name] = s
                        }
                        
                        for j in 0..<selectedSequence.count {
                            let obj = selectedSequence[j]
                            
                            if let nestedSequence = obj as? Sequence {
                                if nestedSequence.isEmpty {
                                    var callableSequences = [Sequence]()
                                    var nextObjects = [Any?]()
                                    var nextSequences = [Sequence]()
                                    
                                    for character in characters {
                                        if name == character.name {
                                            for sourceSequence in character.sequences {
                                                var sequenceStack = self.getSequenceStack(sourceSequence: sourceSequence, targetSequence: nestedSequence)
                                                
                                                if sequenceStack.isEmpty {
                                                    callableSequences.append(sourceSequence)
                                                } else {
                                                    var stack = [Sequence]()
                                                    
                                                    repeat {
                                                        let poppedSequence = sequenceStack.popLast()!
                                                        
                                                        if !sequenceStack.isEmpty {
                                                            var tempStack = [Sequence]()
                                                            
                                                            for sequence in sequenceStack.last! {
                                                                if let s = sequence as? Sequence {
                                                                    if !s.isEmpty && !stack.contains(where: { $0 === s }) {
                                                                        tempStack.append(s)
                                                                    }
                                                                    
                                                                    if s === poppedSequence {
                                                                        break
                                                                    }
                                                                }
                                                            }
                                                            
                                                            while !tempStack.isEmpty {
                                                                stack.append(tempStack.popLast()!)
                                                            }
                                                        }
                                                        
                                                        if !poppedSequence.isEmpty && !stack.contains(where: { $0 === poppedSequence }) {
                                                            stack.append(poppedSequence)
                                                        }
                                                    } while !sequenceStack.isEmpty
                                                    
                                                    while !stack.isEmpty {
                                                        callableSequences.append(stack.popLast()!)
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    
                                    for k in j + 1..<selectedSequence.count {
                                        nextObjects.append(selectedSequence[k])
                                    }
                                    
                                    for k in i + 1..<sequences.count {
                                        nextSequences.append(sequences[k])
                                    }
                                    
                                    executionQueue.append((sequences: callableSequences.filter { $0.name == nestedSequence.name }, state: nestedSequence.state, next: (sequence: tempFlattenedSequence, objects: nextObjects, sequences: nextSequences)))
                                    isAborted = true
                                    
                                    break
                                }
                            } else {
                                tempFlattenedSequence.append(obj)
                            }
                        }
                        
                        if isAborted {
                            break
                        }
                        
                        if !tempFlattenedSequence.isEmpty {
                            preparedSequences.append(tempFlattenedSequence)
                        }
                    }
                } else {
                    for sequence in tempPreparedSequences {
                        preparedSequences.append(sequence)
                    }
                }
            } while !executionQueue.isEmpty
            
            return preparedSequences
        }
        
        private func prepareAsync(characters: [(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, language: String?, prompt: String?, guest: Bool, sequences: [Sequence])], name: String, sequences: [Sequence], state: String? = nil, transform: (([Sequence]) -> [Sequence])? = nil) async -> [Sequence] {
            return await Task.detached {
                var executionQueue: [(sequences: [Sequence], state: String?, next: (sequence: Sequence, objects: [Any?], sequences: [Sequence])?)] = [(sequences: sequences, state: state, next: nil)]
                var preparedSequences = [Sequence]()
                
                await self.innerSemaphore.wait()
                
                repeat {
                    var (currentSequences, currentState, next) = executionQueue.removeFirst()
                    var selectedSequences = [Sequence]()
                    var tempPreparedSequences = [Sequence]()
                    
                    for sequence in currentSequences {
                        if let pattern = sequence.state {
                            if let currentState {
                                if currentState.isEmpty {
                                    await MainActor.run { [weak self] in
                                        if let name = sequence.name, let states = self?.states, states.keys.contains(name) {
                                            self?.states.removeValue(forKey: name)
                                        }
                                    }
                                } else if let regex = try? Regex(pattern), let match = currentState.firstMatch(of: regex), !match.output.isEmpty {
                                    selectedSequences.append(sequence)
                                }
                            } else if let name = sequence.name, let input = self.states[name], let regex = try? Regex(pattern), let match = input.firstMatch(of: regex), !match.output.isEmpty {
                                selectedSequences.append(sequence)
                            }
                        }
                    }
                    
                    if selectedSequences.isEmpty {
                        for sequence in currentSequences {
                            if sequence.state == nil {
                                selectedSequences.append(sequence)
                            }
                        }
                        
                        currentState = nil
                    } else if currentState != nil && currentState!.isEmpty {
                        currentState = nil
                    }
                    
                    if !selectedSequences.isEmpty {
                        if let transform {
                            selectedSequences = transform(selectedSequences)
                        }
                        
                        for i in 0..<selectedSequences.count {
                            let selectedSequence = selectedSequences[i]
                            var isAborted = false
                            let flattenedSequence = Sequence(name: selectedSequence.name, state: selectedSequence.state)
                            
                            if let name = selectedSequence.name, let currentState {
                                await MainActor.run { [weak self] in
                                    self?.states[name] = currentState
                                }
                            }
                            
                            for j in 0..<selectedSequence.count {
                                let obj = selectedSequence[j]
                                
                                if let nestedSequence = obj as? Sequence {
                                    if nestedSequence.isEmpty {
                                        var callableSequences = [Sequence]()
                                        var nextSequence: Sequence
                                        var nextObjects: [Any?]
                                        var nextSequences: [Sequence]
                                        
                                        for character in characters {
                                            if name == character.name {
                                                for sourceSequence in character.sequences {
                                                    var sequenceStack = self.getSequenceStack(sourceSequence: sourceSequence, targetSequence: nestedSequence)
                                                    
                                                    if sequenceStack.isEmpty {
                                                        callableSequences.append(sourceSequence)
                                                    } else {
                                                        var stack = [Sequence]()
                                                        
                                                        repeat {
                                                            let poppedSequence = sequenceStack.popLast()!
                                                            
                                                            if !sequenceStack.isEmpty {
                                                                var tempStack = [Sequence]()
                                                                
                                                                for sequence in sequenceStack.last! {
                                                                    if let s = sequence as? Sequence {
                                                                        if !s.isEmpty && !stack.contains(where: { $0 === s }) {
                                                                            tempStack.append(s)
                                                                        }
                                                                        
                                                                        if s === poppedSequence {
                                                                            break
                                                                        }
                                                                    }
                                                                }
                                                                
                                                                while !tempStack.isEmpty {
                                                                    stack.append(tempStack.popLast()!)
                                                                }
                                                            }
                                                            
                                                            if !poppedSequence.isEmpty && !stack.contains(where: { $0 === poppedSequence }) {
                                                                stack.append(poppedSequence)
                                                            }
                                                        } while !sequenceStack.isEmpty
                                                        
                                                        while !stack.isEmpty {
                                                            callableSequences.append(stack.popLast()!)
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        
                                        if let (sequence, objects, sequences) = next {
                                            nextSequence = sequence
                                            
                                            for o in flattenedSequence {
                                                nextSequence.append(o)
                                            }
                                            
                                            nextObjects = objects
                                            nextSequences = sequences
                                        } else {
                                            nextSequence = flattenedSequence
                                            nextObjects = []
                                            nextSequences = []
                                        }
                                        
                                        for k in j + 1..<selectedSequence.count {
                                            nextObjects.append(selectedSequence[k])
                                        }
                                        
                                        for k in i + 1..<selectedSequences.count {
                                            nextSequences.append(selectedSequences[k])
                                        }
                                        
                                        executionQueue.append((sequences: callableSequences.filter { $0.name == nestedSequence.name }, state: nestedSequence.state, next: (sequence: nextSequence, objects: nextObjects, sequences: nextSequences)))
                                        isAborted = true
                                        
                                        break
                                    }
                                } else {
                                    flattenedSequence.append(obj)
                                }
                            }
                            
                            if isAborted {
                                tempPreparedSequences.removeAll()
                                next = nil
                                
                                break
                            }
                            
                            if !flattenedSequence.isEmpty {
                                tempPreparedSequences.append(flattenedSequence)
                            }
                        }
                    }
                    
                    if let (flattenedSequence, objects, sequences) = next {
                        var isAborted = false
                        
                        for s in tempPreparedSequences {
                            for o in s {
                                flattenedSequence.append(o)
                            }
                        }
                        
                        for i in 0..<objects.count {
                            let obj = objects[i]
                            
                            if let nestedSequence = obj as? Sequence {
                                if nestedSequence.isEmpty {
                                    var callableSequences = [Sequence]()
                                    var nextObjects = [Any?]()
                                    
                                    for character in characters {
                                        if name == character.name {
                                            for sourceSequence in character.sequences {
                                                var sequenceStack = self.getSequenceStack(sourceSequence: sourceSequence, targetSequence: nestedSequence)
                                                
                                                if sequenceStack.isEmpty {
                                                    callableSequences.append(sourceSequence)
                                                } else {
                                                    var stack = [Sequence]()
                                                    
                                                    repeat {
                                                        let poppedSequence = sequenceStack.popLast()!
                                                        
                                                        if !sequenceStack.isEmpty {
                                                            var tempStack = [Sequence]()
                                                            
                                                            for sequence in sequenceStack.last! {
                                                                if let s = sequence as? Sequence {
                                                                    if !s.isEmpty && !stack.contains(where: { $0 === s }) {
                                                                        tempStack.append(s)
                                                                    }
                                                                    
                                                                    if s === poppedSequence {
                                                                        break
                                                                    }
                                                                }
                                                            }
                                                            
                                                            while !tempStack.isEmpty {
                                                                stack.append(tempStack.popLast()!)
                                                            }
                                                        }
                                                        
                                                        if !poppedSequence.isEmpty && !stack.contains(where: { $0 === poppedSequence }) {
                                                            stack.append(poppedSequence)
                                                        }
                                                    } while !sequenceStack.isEmpty
                                                    
                                                    while !stack.isEmpty {
                                                        callableSequences.append(stack.popLast()!)
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    
                                    for j in i + 1..<objects.count {
                                        nextObjects.append(objects[j])
                                    }
                                    
                                    executionQueue.append((sequences: callableSequences.filter { $0.name == nestedSequence.name }, state: nestedSequence.state, next: (sequence: flattenedSequence, objects: nextObjects, sequences: sequences)))
                                    isAborted = true
                                    
                                    break
                                }
                            } else {
                                flattenedSequence.append(obj)
                            }
                        }
                        
                        if isAborted {
                            continue
                        }
                        
                        if !flattenedSequence.isEmpty {
                            preparedSequences.append(flattenedSequence)
                        }
                        
                        for i in 0..<sequences.count {
                            let selectedSequence = sequences[i]
                            let tempFlattenedSequence = Sequence(name: selectedSequence.name, state: selectedSequence.state)
                            
                            if let name = selectedSequence.name, let s = currentState {
                                await MainActor.run { [weak self] in
                                    self?.states[name] = s
                                }
                            }
                            
                            for j in 0..<selectedSequence.count {
                                let obj = selectedSequence[j]
                                
                                if let nestedSequence = obj as? Sequence {
                                    if nestedSequence.isEmpty {
                                        var callableSequences = [Sequence]()
                                        var nextObjects = [Any?]()
                                        var nextSequences = [Sequence]()
                                        
                                        for character in characters {
                                            if name == character.name {
                                                for sourceSequence in character.sequences {
                                                    var sequenceStack = self.getSequenceStack(sourceSequence: sourceSequence, targetSequence: nestedSequence)
                                                    
                                                    if sequenceStack.isEmpty {
                                                        callableSequences.append(sourceSequence)
                                                    } else {
                                                        var stack = [Sequence]()
                                                        
                                                        repeat {
                                                            let poppedSequence = sequenceStack.popLast()!
                                                            
                                                            if !sequenceStack.isEmpty {
                                                                var tempStack = [Sequence]()
                                                                
                                                                for sequence in sequenceStack.last! {
                                                                    if let s = sequence as? Sequence {
                                                                        if !s.isEmpty && !stack.contains(where: { $0 === s }) {
                                                                            tempStack.append(s)
                                                                        }
                                                                        
                                                                        if s === poppedSequence {
                                                                            break
                                                                        }
                                                                    }
                                                                }
                                                                
                                                                while !tempStack.isEmpty {
                                                                    stack.append(tempStack.popLast()!)
                                                                }
                                                            }
                                                            
                                                            if !poppedSequence.isEmpty && !stack.contains(where: { $0 === poppedSequence }) {
                                                                stack.append(poppedSequence)
                                                            }
                                                        } while !sequenceStack.isEmpty
                                                        
                                                        while !stack.isEmpty {
                                                            callableSequences.append(stack.popLast()!)
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        
                                        for k in j + 1..<selectedSequence.count {
                                            nextObjects.append(selectedSequence[k])
                                        }
                                        
                                        for k in i + 1..<sequences.count {
                                            nextSequences.append(sequences[k])
                                        }
                                        
                                        executionQueue.append((sequences: callableSequences.filter { $0.name == nestedSequence.name }, state: nestedSequence.state, next: (sequence: tempFlattenedSequence, objects: nextObjects, sequences: nextSequences)))
                                        isAborted = true
                                        
                                        break
                                    }
                                } else {
                                    tempFlattenedSequence.append(obj)
                                }
                            }
                            
                            if isAborted {
                                break
                            }
                            
                            if !tempFlattenedSequence.isEmpty {
                                preparedSequences.append(tempFlattenedSequence)
                            }
                        }
                    } else {
                        for sequence in tempPreparedSequences {
                            preparedSequences.append(sequence)
                        }
                    }
                } while !executionQueue.isEmpty
                
                await self.innerSemaphore.signal()
                
                return preparedSequences
            }.value
        }
        
        private func prepareAsync(characters: [(name: String, path: String, location: CGPoint, size: CGSize, scale: Double, language: String?, prompt: String?, guest: Bool, sequences: [Sequence])], name: String, sequences: [Sequence], state: String? = nil, scores: [String: (Double, [String]?, String?, Date)], words: [Word], temperature: Double = 1.0, beamWidth: Int = 3) async -> [Sequence] {
            return await Task.detached {
                let epsilon = powl(10, -6)
                var preparedSequences = [Sequence]()
                var data = [Int: [(Sequence, ([Word], Set<String>))]]()
                var reverseDictionary = [String: [(String, [String])]]()
                
                for (key, value) in scores {
                    if let attributes = value.1 {
                        for attribute in attributes {
                            if var tuple = reverseDictionary[attribute] {
                                tuple.append((key, attributes))
                                reverseDictionary[attribute] = tuple
                            } else {
                                reverseDictionary[attribute] = [(key, attributes)]
                            }
                        }
                    }
                }
                
                repeat {
                    var i = 0
                    var wordQueue: [Word]? = nil
                    var attributeSet = Set<String>()
                    var isAvailable = true
                    
                    await self.outerSemaphore.wait()
                    
                    let states = Swift.Dictionary<String, String>(uniqueKeysWithValues: zip(self.states.keys, self.states.values))
                    
                    preparedSequences.append(contentsOf: await self.prepareAsync(characters: characters, name: name, sequences: sequences, state: state, transform: { sourceSequences -> [Sequence] in
                        var targetSequences = [Sequence]()
                        
                        if let tuples = data[i] {
                            let first = tuples.first!
                            
                            targetSequences.append(first.0)
                            wordQueue = first.1.0
                            
                            for s in first.1.1 {
                                if !attributeSet.contains(s) {
                                    attributeSet.insert(s)
                                }
                            }
                        } else {
                            var tuples = [(Sequence, ([Word], Set<String>))]()
                            
                            for sequence in sourceSequences {
                                var tempWordQueue = [Word](wordQueue ?? words)
                                var tempAttributeSet = Set<String>(attributeSet)
                                var appendRequired = true
                                
                                for obj in sequence {
                                    if let message = obj as? Message {
                                        var input = String(message.content)
                                        
                                        repeat {
                                            if let match = input.firstMatch(of: /({{1,2})([^{}]+)(}{1,2})/) {
                                                if match.output.1.count < 2 || match.output.3.count < 2 {
                                                    let attribute = String(match.output.2)
                                                    
                                                    if !tempAttributeSet.contains(attribute) {
                                                        if !tempWordQueue.isEmpty {
                                                            repeat {
                                                                if let attributes = tempWordQueue.removeFirst().attributes, attributes.contains(attribute) {
                                                                    tempAttributeSet.insert(attribute)
                                                                    
                                                                    break
                                                                }
                                                            } while !tempWordQueue.isEmpty
                                                            
                                                            if tempAttributeSet.contains(attribute) {
                                                                continue
                                                            }
                                                        }
                                                        
                                                        appendRequired = false
                                                        
                                                        break
                                                    }
                                                }
                                                
                                                input = String(input[match.range.upperBound..<input.endIndex])
                                            } else {
                                                input.removeAll()
                                            }
                                        } while !input.isEmpty
                                    }
                                }
                                
                                if appendRequired {
                                    tuples.append((sequence, (tempWordQueue, tempAttributeSet)))
                                }
                            }
                            
                            if tuples.isEmpty {
                                isAvailable = false
                            } else {
                                tuples.shuffle()
                                
                                let first = tuples.first!
                                
                                targetSequences.append(first.0)
                                wordQueue = first.1.0
                                
                                for s in first.1.1 {
                                    if !attributeSet.contains(s) {
                                        attributeSet.insert(s)
                                    }
                                }
                                
                                data[i] = tuples
                            }
                        }
                        
                        i += 1
                        
                        return targetSequences
                    }))
                    
                    if isAvailable && !words.isEmpty == preparedSequences.contains(where: { sequence in
                        for obj in sequence {
                            if let message = obj as? Message {
                                if let match = message.content.firstMatch(of: /({{1,2})[^{}]+(}{1,2})/), match.output.1.count < 2 || match.output.2.count < 2 {
                                    return true
                                }
                                
                                return false
                            }
                        }
                        
                        return false
                    }) && words.contains(where: { word in
                        if let attributes = word.attributes {
                            for attribute in attributes {
                                if attributeSet.contains(attribute) {
                                    return true
                                }
                            }
                        }
                        
                        return false
                    }) == !attributeSet.isEmpty {
                        var replacementCache = [String: (text: String, attributes: [String])]()
                        var tempAttributeSet = Set<String>()
                        var outputs = [([(text: String, attributes: [String]?)], [String: (text: String, attributes: [String])], Double)]()
                        
                        for sequence in preparedSequences {
                            var segments = [Any?]()
                            
                            for obj in sequence {
                                if let message = obj as? Message, message.allSatisfy({ $0.attributes == nil }) {
                                    var input = String(message.content)
                                    var output = String()
                                    
                                    outputs.append(([], Swift.Dictionary<String, (text: String, attributes: [String])>(uniqueKeysWithValues: zip(replacementCache.keys, replacementCache.values)), 1.0))
                                    
                                    repeat {
                                        if let match = input.firstMatch(of: /({{1,2})([^{}]+)(}{1,2})/) {
                                            let attributePattern = String(match.output.2).trimmingCharacters(in: .whitespaces)
                                            
                                            output.append(String(input[input.startIndex..<match.range.lowerBound]))
                                            
                                            if match.output.1.count == 2 {
                                                if match.output.3.count == 2 {
                                                    output.append("{\(attributePattern)}")
                                                    input = String(input[match.range.upperBound..<input.endIndex])
                                                    
                                                    continue
                                                } else {
                                                    output.append("{")
                                                }
                                            }
                                            
                                            if tempAttributeSet.contains(attributePattern) {
                                                for j in 0..<outputs.count {
                                                    outputs[j].0.append((text: output, attributes: nil))
                                                    outputs[j].0.append(outputs[j].1[attributePattern]!)
                                                }
                                                
                                                output.removeAll()
                                            } else if let regex = try? Regex(attributePattern) {
                                                var selections = [(String, [String])]()
                                                var probabilities = [Double]()
                                                var candidates = [([(text: String, attributes: [String]?)], [String: (text: String, attributes: [String])], (text: String, attributes: [String]), Double)]()
                                                
                                                for word in words {
                                                    if let attributes = word.attributes {
                                                        for attribute in attributes {
                                                            if let match = attribute.firstMatch(of: regex), !match.output.isEmpty {
                                                                selections.append((word.name, attributes))
                                                                
                                                                if let (score, _, _, _) = scores[word.name] {
                                                                    probabilities.append(score)
                                                                } else {
                                                                    probabilities.append(epsilon)
                                                                }
                                                                
                                                                break
                                                            }
                                                        }
                                                    }
                                                }
                                                
                                                if selections.isEmpty {
                                                    for key in reverseDictionary.keys {
                                                        if let match = key.firstMatch(of: regex), !match.output.isEmpty {
                                                            for (name, attributes) in reverseDictionary[key]! {
                                                                selections.append((name, attributes))
                                                                
                                                                if let (score, _, _, _) = scores[name] {
                                                                    probabilities.append(score)
                                                                } else {
                                                                    probabilities.append(epsilon)
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                
                                                probabilities = self.softmax(x: probabilities, temperature: max(temperature, epsilon))
                                                
                                                for j in 0..<outputs.count {
                                                    outputs[j].0.append((text: output, attributes: nil))
                                                    
                                                    for k in 0..<probabilities.count {
                                                        if !outputs[j].1.values.contains(where: { $0.text == selections[k].0 }) {
                                                            candidates.append((outputs[j].0, outputs[j].1, (text: selections[k].0, attributes: selections[k].1), outputs[j].2 * probabilities[k]))
                                                        }
                                                    }
                                                }
                                                
                                                output.removeAll()
                                                outputs.removeAll()
                                                candidates.sort { $0.3 > $1.3 }
                                                
                                                for j in 0..<min(beamWidth, candidates.count) {
                                                    var tempInlines = candidates[j].0
                                                    var tempReplacementCache = Swift.Dictionary<String, (text: String, attributes: [String])>(uniqueKeysWithValues: zip(candidates[j].1.keys, candidates[j].1.values))
                                                    
                                                    tempInlines.append(candidates[j].2)
                                                    tempReplacementCache[attributePattern] = candidates[j].2
                                                    outputs.append((tempInlines, tempReplacementCache, candidates[j].3))
                                                }
                                                
                                                tempAttributeSet.insert(attributePattern)
                                            }
                                            
                                            if match.output.3.count == 2 {
                                                output.append("}")
                                            }
                                            
                                            input = String(input[match.range.upperBound..<input.endIndex])
                                        } else {
                                            output.append(input)
                                            input.removeAll()
                                        }
                                    } while !input.isEmpty
                                    
                                    var probabilities = [Double]()
                                    
                                    for (_, _, probability) in outputs {
                                        probabilities.append(probability)
                                    }
                                    
                                    probabilities = self.softmax(x: probabilities, temperature: max(temperature, epsilon))
                                    
                                    let tuple = outputs[min(self.choice(probabilities: probabilities), probabilities.count - 1)]
                                    var m = Message(id: message.id)
                                    
                                    m.speed = message.speed
                                    m.duration = message.duration
                                    
                                    for inline in tuple.0 {
                                        m.append(inline)
                                    }
                                    
                                    if !output.isEmpty {
                                        m.append((text: output, attributes: nil))
                                    }
                                    
                                    for (key, value) in tuple.1 {
                                        replacementCache[key] = value
                                    }
                                    
                                    segments.append(m)
                                } else {
                                    segments.append(obj)
                                }
                            }
                            
                            sequence.removeAll()
                            
                            for obj in segments {
                                sequence.append(obj)
                            }
                        }
                        
                        await self.outerSemaphore.signal()
                        
                        break
                    }
                    
                    preparedSequences.removeAll()
                    
                    await MainActor.run { [weak self] in
                        self?.states.removeAll()
                        
                        for (key, value) in states {
                            self?.states[key] = value
                        }
                    }
                    
                    await self.outerSemaphore.signal()
                    
                    for j in stride(from: i - 1, through: 0, by: -1) {
                        if var tuples = data[j] {
                            tuples.removeFirst()
                            
                            if tuples.count > 0 {
                                data[j] = tuples
                                
                                break
                            } else {
                                data.removeValue(forKey: j)
                            }
                        }
                    }
                } while !data.isEmpty
                
                return preparedSequences
            }.value
        }
        
        private func getSequenceStack(sourceSequence: Sequence, targetSequence: Sequence) -> [Sequence] {
            var sequenceStack = [Sequence]()
            var sequenceQueue: [(index: Int, sequence: Sequence)] = [(index: -1, sequence: sourceSequence)]
            var tracks = [(parent: Int, next: Sequence)]()
            
            repeat {
                let (parentIndex, nextSourceSequence) = sequenceQueue.removeFirst()
                let index = tracks.count
                
                tracks.append((parent: parentIndex, next: nextSourceSequence))
                
                if nextSourceSequence !== targetSequence {
                    for o in nextSourceSequence {
                        if let sequence = o as? Sequence {
                            if sequence === targetSequence {
                                var nextIndex = index
                                
                                sequenceStack.insert(sequence, at: 0)
                                
                                repeat {
                                    let track = tracks[nextIndex]
                                    
                                    sequenceStack.insert(track.next, at: 0)
                                    nextIndex = track.parent
                                } while nextIndex >= 0
                                
                                sequenceQueue.removeAll()
                                
                                break
                            } else {
                                tracks.append((parent: index, next: sequence))
                                sequenceQueue.append((index: index, sequence: sequence))
                            }
                        }
                    }
                }
            } while !sequenceQueue.isEmpty
            
            return sequenceStack
        }
        
        private func choice(probabilities: [Double]) -> Int {
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
        
        private func softmax(x: [Double], temperature: Double = 1.0) -> [Double] {
            var q = [Double]()
            var max = -Double.greatestFiniteMagnitude
            var sum = 0.0
            
            for z in x {
                if z > max {
                    max = z
                }
            }
            
            for z in x {
                sum += exp((z - max) / temperature);
            }
            
            for z in x {
                q.append(exp((z - max) / temperature) / sum)
            }
            
            return q
        }
    }
    
    public class Parser: NSObject, XMLParserDelegate {
        public var excludeSequences = false
        private var workingStack: [Any]? = nil
        private var characters = [(id: String?, name: String, location: CGPoint, size: CGSize, scale: Double, language: String?, preview: String?, prompt: String?, sequences: [Sequence], types: [String: (Int, Set<Int>)], insets: (top: Double, left: Double, bottom: Double, right: Double))]()
        private var attributes = [String]()
        
        public func parse(path: String) -> ([(id: String?, name: String, location: CGPoint, size: CGSize, scale: Double, language: String?, preview: String?, prompt: String?, sequences: [Sequence], types: [String: (Int, Set<Int>)], insets: (top: Double, left: Double, bottom: Double, right: Double))], [String]) {
            if let file = FileHandle(forReadingAtPath: path) {
                var isXml = false
                
                defer {
                    try? file.close()
                }
                
                if let data = try? file.read(upToCount: 4096) {
                    var text = String(decoding: data, as: UTF8.self)
                    
                    text = text.trimmingCharacters(in: .whitespacesAndNewlines)
                    
                    if text.hasPrefix("\u{FEFF}") {
                        text.removeFirst()
                    }
                    
                    if text.hasPrefix("<?xml") {
                        isXml = true
                    } else if let first = text.first, first == "<" {
                        isXml = true
                    }
                    
                    try? file.seek(toOffset: 0)
                }
                
                if let data = try? file.readToEnd() {
                    if isXml {
                        let parser = XMLParser(data: data)
                        
                        parser.delegate = self
                        parser.parse()
                    } else if let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [String: Any] {
                        if let name = jsonRoot["name"] as? String, let width = jsonRoot["width"] as? Double, let height = jsonRoot["height"] as? Double {
                            var sequences = [Sequence]()
                            
                            if !self.excludeSequences, let animations = jsonRoot["animations"] as? [[String: Any]] {
                                for animation in animations {
                                    let sequence = Sequence(name: animation["name"] as? String, state: animation["state"] as? String)
                                    let repeats: UInt
                                    var caches = [String: (Int, String?, [Sprite])]()
                                    var animations = [Animation]()
                                    
                                    if let value = animation["repeats"] as? Double {
                                        repeats = UInt(value)
                                    } else {
                                        repeats = 1
                                    }
                                    
                                    if let frames = animation["frames"] as? [[String: Any]] {
                                        for frame in frames {
                                            var sprite = Sprite()
                                            let z: Int
                                            let type: String?
                                            let key: String
                                            
                                            if let x = frame["x"] as? Double {
                                                sprite.location.x = x
                                            }
                                            
                                            if let y = frame["y"] as? Double {
                                                sprite.location.y = y
                                            }
                                            
                                            if let width = frame["width"] as? Double {
                                                sprite.size.width = width
                                            }
                                            
                                            if let height = frame["height"] as? Double {
                                                sprite.size.height = height
                                            }
                                            
                                            if let opacity = frame["opacity"] as? Double {
                                                sprite.opacity = opacity
                                            }
                                            
                                            if let delay = frame["delay"] as? Double {
                                                sprite.delay = delay
                                            }
                                            
                                            if let url = frame["url"] as? String {
                                                sprite.path = url
                                            }
                                            
                                            if let value = frame["z"] as? Double {
                                                z = Int(value)
                                            } else {
                                                z = 0
                                            }
                                            
                                            if let value = frame["type"] as? String {
                                                type = value
                                                key = "\(z)&\(value)"
                                            } else {
                                                type = nil
                                                key = "\(z)"
                                            }
                                            
                                            if let tuple = caches[key] {
                                                var sprites = tuple.2
                                                
                                                sprites.append(sprite)
                                                caches[key] = (tuple.0, tuple.1, sprites)
                                            } else {
                                                caches[key] = (z, type, [sprite])
                                            }
                                        }
                                    }
                                    
                                    for (_, value) in caches {
                                        let a = Animation(frames: value.2)
                                        
                                        a.repeats = repeats
                                        a.z = value.0
                                        a.type = value.1
                                        
                                        animations.append(a)
                                    }
                                    
                                    sequence.append(animations)
                                    sequences.append(sequence)
                                }
                            }
                            
                            self.characters.append((id: nil, name: name, location: CGPoint(x: jsonRoot["x"] as? Double ?? 0.0, y: jsonRoot["y"] as? Double ?? 0.0), size: CGSize(width: width, height: height), scale: jsonRoot["scale"] as? Double ?? 1.0, language: jsonRoot["language"] as? String, preview: jsonRoot["preview"] as? String, prompt: jsonRoot["prompt"] as? String, sequences: sequences, types: [:], insets: (top: Double.greatestFiniteMagnitude, left: Double.greatestFiniteMagnitude, bottom: -Double.greatestFiniteMagnitude, right: -Double.greatestFiniteMagnitude)))
                        }
                    }
                }
            }
            
            for i in 0..<self.characters.count {
                var sequenceQueue = self.characters[i].sequences
                var order = 0
                
                while !sequenceQueue.isEmpty {
                    let sequence = sequenceQueue.removeFirst()
                    
                    for obj in sequence {
                        if let s = obj as? Sequence {
                            sequenceQueue.append(s)
                        } else if let animations = obj as? [Animation] {
                            for animation in animations {
                                if let type = animation.type {
                                    if var tuple = self.characters[i].types[type] {
                                        if !tuple.1.contains(animation.z) {
                                            tuple.1.insert(animation.z)
                                            self.characters[i].types[type] = tuple
                                        }
                                    } else {
                                        self.characters[i].types[type] = (order, [animation.z])
                                        order += 1
                                    }
                                }
                                
                                if animation.z >= 0 {
                                    for sprite in animation {
                                        let right = sprite.location.x + sprite.size.width
                                        let bottom = sprite.location.y + sprite.size.height
                                        
                                        if sprite.location.x < self.characters[i].insets.left {
                                            self.characters[i].insets.left = sprite.location.x
                                        }
                                        
                                        if right > self.characters[i].insets.right {
                                            self.characters[i].insets.right = right
                                        }
                                        
                                        if sprite.location.y < self.characters[i].insets.top {
                                            self.characters[i].insets.top = sprite.location.y
                                        }
                                        
                                        if bottom > self.characters[i].insets.bottom {
                                            self.characters[i].insets.bottom = bottom
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                self.characters[i].insets.left += self.characters[i].location.x
                self.characters[i].insets.right += self.characters[i].location.x
                self.characters[i].insets.top += self.characters[i].location.y
                self.characters[i].insets.bottom += self.characters[i].location.y
                
                if let prompt = self.characters[i].prompt {
                    let p = URL(filePath: path).deletingLastPathComponent().appending(path: prompt, directoryHint: .inferFromPath).path(percentEncoded: false)
                    
                    if FileManager.default.fileExists(atPath: p), let file = FileHandle(forReadingAtPath: p) {
                        defer {
                            try? file.close()
                        }
                        
                        if let data = try? file.readToEnd(), let s = String(data: data, encoding: .utf8) {
                            self.characters[i].prompt = s
                        }
                    }
                }
            }
            
            return (self.characters, self.attributes)
        }
        
        public func parserDidStartDocument(_ parser: XMLParser) {
            self.workingStack = []
        }
        
        public func parser(_ parser: XMLParser, didStartElement elementName: String, namespaceURI: String?, qualifiedName qName: String?, attributes attributeDict: [String : String] = [:]) {
            if elementName == "character" {
                if let name = attributeDict["name"], var workingStack = self.workingStack, workingStack.isEmpty {
                    var location = CGPoint()
                    var size = CGSize()
                    var scale = 1.0
                    
                    if let x = attributeDict["x"] {
                        location.x = Double(x)!
                    }
                    
                    if let y = attributeDict["y"] {
                        location.y = Double(y)!
                    }
                    
                    if let width = attributeDict["width"] {
                        size.width = Double(width)!
                    }
                    
                    if let height = attributeDict["height"] {
                        size.height = Double(height)!
                    }
                    
                    if let s = attributeDict["scale"] {
                        scale = Double(s)!
                    }
                    
                    if self.excludeSequences {
                        self.characters.append((id: attributeDict["id"], name: name, location: location, size: size, scale: scale, language: attributeDict["language"], preview: attributeDict["preview"], prompt: attributeDict["prompt"], sequences: [Sequence](), types: [:], insets: (top: Double.greatestFiniteMagnitude, left: Double.greatestFiniteMagnitude, bottom: -Double.greatestFiniteMagnitude, right: -Double.greatestFiniteMagnitude)))
                        parser.abortParsing()
                    } else {
                        workingStack.append((id: attributeDict["id"], name: name, location: location, size: size, scale: scale, language: attributeDict["language"], preview: attributeDict["preview"], prompt: attributeDict["prompt"], sequences: [Sequence]()))
                        self.workingStack = workingStack
                    }
                }
            } else if elementName == "sequence" {
                if let name = attributeDict["name"], var workingStack = self.workingStack {
                    let sequence = Sequence(name: name)
                    
                    if let state = attributeDict["state"] {
                        sequence.state = state
                    }
                    
                    workingStack.append(sequence)
                    self.workingStack = workingStack
                }
            } else if elementName == "message" {
                if var workingStack = self.workingStack {
                    var message = Message()
                    
                    if let speed = attributeDict["speed"] {
                        message.speed = Double(speed)!
                    }
                    
                    if let duration = attributeDict["duration"], let match = duration.wholeMatch(of: /([0-9]{2}):([0-9]{2}):(\d+)/) {
                        let hours = Int(match.output.1)! * 60 * 60
                        let minutes = Int(match.output.2)! * 60
                        let seconds = Double(match.output.3)!
                        
                        message.duration = Double(hours) + Double(minutes) + seconds
                    }
                    
                    workingStack.append(message)
                    self.workingStack = workingStack
                }
            } else if elementName == "break" || elementName == "br" {
                if var workingStack = self.workingStack {
                    workingStack.append("\n")
                    self.workingStack = workingStack
                }
            } else if elementName == "animation" || elementName == "motion" {
                if var workingStack = self.workingStack {
                    let animation = Animation()
                    var iterations: UInt = 1
                    
                    if let repeats = attributeDict["repeats"] {
                        iterations = UInt(repeats)!
                        animation.repeats = min(iterations, 1)
                    }
                    
                    if let z = attributeDict["z"] {
                        animation.z = Int(z)!
                    }
                    
                    if let type = attributeDict["type"] {
                        animation.type = type
                    }
                    
                    workingStack.append((animation, iterations))
                    self.workingStack = workingStack
                }
            } else if elementName == "frame" || elementName == "image" || elementName == "sprite" {
                if var workingStack = self.workingStack {
                    var sprite = Sprite()
                    
                    if let x = attributeDict["x"] {
                        sprite.location.x = Double(x)!
                    }
                    
                    if let y = attributeDict["y"] {
                        sprite.location.y = Double(y)!
                    }
                    
                    if let width = attributeDict["width"] {
                        sprite.size.width = Double(width)!
                    }
                    
                    if let height = attributeDict["height"] {
                        sprite.size.height = Double(height)!
                    }
                    
                    if let opacity = attributeDict["opacity"] {
                        sprite.opacity = Double(opacity)!
                    }
                    
                    if let delay = attributeDict["delay"] {
                        sprite.delay = Double(delay)!
                    }
                    
                    workingStack.append(sprite)
                    self.workingStack = workingStack
                }
            } else if elementName == "sound", var workingStack = self.workingStack {
                workingStack.append(Sound())
                self.workingStack = workingStack
            }
        }
        
        public func parser(_ parser: XMLParser, foundCharacters string: String) {
            if !string.isEmpty, var workingStack = self.workingStack {
                workingStack.append(string)
                self.workingStack = workingStack
            }
        }
        
        public func parser(_ parser: XMLParser, didEndElement elementName: String, namespaceURI: String?, qualifiedName qName: String?) {
            if elementName == "character" {
                if var workingStack = self.workingStack {
                    while !workingStack.isEmpty {
                        if let character = workingStack.last as? (id: String?, name: String, location: CGPoint, size: CGSize, scale: Double, language: String?, preview: String?, prompt: String?, sequences: [Sequence]) {
                            self.characters.append((id: character.id, name: character.name, location: character.location, size: character.size, scale: character.scale, language: character.language, preview: character.preview, prompt: character.prompt, sequences: character.sequences, types: [:], insets: (top: Double.greatestFiniteMagnitude, left: Double.greatestFiniteMagnitude, bottom: -Double.greatestFiniteMagnitude, right: -Double.greatestFiniteMagnitude)))
                            
                            break
                        } else {
                            workingStack.removeLast()
                        }
                    }
                    
                    self.workingStack = workingStack
                }
            } else if elementName == "sequence" {
                if var workingStack = self.workingStack {
                    var stack = [Any]()
                    
                    while !workingStack.isEmpty {
                        let obj = workingStack.popLast()!
                        
                        if var childSequence = obj as? Sequence {
                            for i in stride(from: workingStack.count - 1, through: 0, by: -1) {
                                if var character = workingStack[i] as? (id: String?, name: String, location: CGPoint, size: CGSize, scale: Double, language: String?, preview: String?, prompt: String?, sequences: [Sequence]) {
                                    var isChildSequence = true
                                    var tempStack = [Any]()
                                    
                                    while !stack.isEmpty {
                                        let o = stack.popLast()!
                                        
                                        if o is Animation || o is Message || o is Sound {
                                            childSequence.append(o)
                                        }
                                    }
                                    
                                    while !workingStack.isEmpty {
                                        if let parentSequence = workingStack.last! as? Sequence {
                                            workingStack.removeLast()
                                            
                                            while !tempStack.isEmpty {
                                                let o = tempStack.popLast()!
                                                
                                                if o is Animation || o is Message || o is Sound {
                                                    parentSequence.append(o)
                                                }
                                            }
                                            
                                            parentSequence.append(self.format(sequence: &childSequence))
                                            workingStack.append(parentSequence)
                                            isChildSequence = false
                                            
                                            break
                                        } else if workingStack.last! is (id: String?, name: String, location: CGPoint, size: CGSize, scale: Double, language: String?, preview: String?, prompt: String?, sequences: [Sequence]) {
                                            break
                                        } else {
                                            tempStack.append(workingStack.popLast()!)
                                        }
                                    }
                                    
                                    if isChildSequence {
                                        character.sequences.append(self.format(sequence: &childSequence))
                                        workingStack[i] = character
                                    }
                                    
                                    break
                                }
                            }
                            
                            break
                        } else {
                            stack.append(obj)
                        }
                    }
                    
                    self.workingStack = workingStack
                }
            } else if elementName == "message" {
                if var workingStack = self.workingStack {
                    var stack = [Any]()
                    
                    while !workingStack.isEmpty {
                        if var message = workingStack.last! as? Message {
                            var text = String()
                            
                            while !stack.isEmpty {
                                if let s = stack.last! as? String {
                                    stack.removeLast()
                                    text.append(s)
                                } else {
                                    break
                                }
                            }
                            
                            text = text.trimmingCharacters(in: .whitespacesAndNewlines)
                            
                            if !text.isEmpty {
                                var input = String(text)
                                
                                repeat {
                                    if let match = input.firstMatch(of: /({{1,2})([^{}]+)(}{1,2})/) {
                                        if match.output.1.count < 2 || match.output.3.count < 2 {
                                            let attribute = String(match.output.2)
                                            
                                            if !self.attributes.contains(attribute) {
                                                self.attributes.append(attribute)
                                            }
                                        }
                                        
                                        input = String(input[match.range.upperBound..<input.endIndex])
                                    } else {
                                        input.removeAll()
                                    }
                                } while !input.isEmpty
                                
                                message.append((text: text, attributes: nil))
                                workingStack[workingStack.count - 1] = message
                            }
                            
                            break
                        } else {
                            stack.append(workingStack.popLast()!)
                        }
                    }
                    
                    self.workingStack = workingStack
                }
            } else if elementName == "animation" || elementName == "motion" {
                if var workingStack = self.workingStack {
                    var stack = [Any]()
                    
                    while !workingStack.isEmpty {
                        if let (animation, iterations) = workingStack.last! as? (Animation, UInt) {
                            var sprites = [Sprite]()
                            
                            while !stack.isEmpty {
                                if let sprite = stack.last! as? Sprite {
                                    stack.removeLast()
                                    sprites.append(sprite)
                                } else if stack.last! is String {
                                    stack.removeLast()
                                } else {
                                    break
                                }
                            }
                            
                            if !sprites.isEmpty {
                                for _ in 0..<max(iterations, 1) {
                                    for sprite in sprites {
                                        animation.append(sprite)
                                    }
                                }
                                
                                workingStack[workingStack.count - 1] = animation
                            }
                            
                            break
                        } else {
                            stack.append(workingStack.popLast()!)
                        }
                    }
                    
                    self.workingStack = workingStack
                }
            } else if elementName == "sprite" || elementName == "image" {
                if var workingStack = self.workingStack {
                    var stack = [Any]()
                    
                    while !workingStack.isEmpty {
                        if var sprite = workingStack.last! as? Sprite {
                            var path = String()
                            
                            while !stack.isEmpty {
                                if let s = stack.last! as? String {
                                    stack.removeLast()
                                    path.append(s)
                                } else {
                                    break
                                }
                            }
                            
                            path = path.trimmingCharacters(in: .whitespacesAndNewlines)
                            
                            if !path.isEmpty {
                                sprite.path = path
                                workingStack[workingStack.count - 1] = sprite
                            }
                            
                            break
                        } else {
                            stack.append(workingStack.popLast()!)
                        }
                    }
                    
                    self.workingStack = workingStack
                }
            } else if elementName == "sound", var workingStack = self.workingStack {
                var stack = [Any]()
                
                while !workingStack.isEmpty {
                    if var sound = workingStack.last! as? Sound {
                        var path = String()
                        
                        while !stack.isEmpty {
                            if let s = stack.last! as? String {
                                stack.removeLast()
                                path.append(s)
                            } else {
                                break
                            }
                        }
                        
                        path = path.trimmingCharacters(in: .whitespacesAndNewlines)
                        
                        if !path.isEmpty {
                            sound.path = path
                            workingStack[workingStack.count - 1] = sound
                        }
                        
                        break
                    } else {
                        stack.append(workingStack.popLast()!)
                    }
                }
                
                self.workingStack = workingStack
            }
        }
        
        public func parserDidEndDocument(_ parser: XMLParser) {
            self.workingStack = nil
        }
        
        private func format(sequence: inout Sequence) -> Sequence {
            var queue = [Any?]()
            var array = [Any?]()
            
            for obj in sequence {
                queue.append(obj)
            }
            
            while !queue.isEmpty {
                let obj = queue.removeFirst()
                
                if let animation = obj as? Animation {
                    var cachedAnimations = [Int: [Animation]]()
                    var animations1 = [Animation]()
                    
                    cachedAnimations[animation.z] = [animation]
                    
                    while !queue.isEmpty {
                        if let a = queue[0] as? Animation {
                            queue.removeFirst()
                            
                            if var animations2 = cachedAnimations[a.z] {
                                animations2.append(a)
                                cachedAnimations[a.z] = animations2
                            } else {
                                cachedAnimations[a.z] = [a]
                            }
                        } else {
                            break
                        }
                    }
                    
                    for key in cachedAnimations.keys {
                        repeat {
                            var animationQueue = [Animation]()
                            let type = cachedAnimations[key]!.first!.type
                            var indexes = [Int]()
                            
                            animationQueue.append(cachedAnimations[key]!.first!)
                            cachedAnimations[key]!.removeFirst()
                            
                            for i in 0..<cachedAnimations[key]!.count {
                                let a = cachedAnimations[key]![i]
                                
                                if type != nil && a.type != nil && type == a.type || type == nil && a.type == nil {
                                    animationQueue.append(a)
                                    indexes.append(i)
                                }
                            }
                            
                            indexes.sort { $0 > $1 }
                            
                            for index in indexes {
                                cachedAnimations[key]!.remove(at: index)
                            }
                            
                            repeat {
                                let dequeuedAnimation = animationQueue.removeFirst()
                                var animations2 = [Animation]()
                                
                                while !animationQueue.isEmpty {
                                    if dequeuedAnimation.repeats == animationQueue.first!.repeats {
                                        animations2.append(animationQueue.removeFirst())
                                    } else {
                                        break
                                    }
                                }
                                
                                for a in animations2 {
                                    for sprite in a {
                                        dequeuedAnimation.append(sprite)
                                    }
                                }
                                
                                animations1.append(dequeuedAnimation)
                            } while !animationQueue.isEmpty
                        } while !cachedAnimations[key]!.isEmpty
                    }
                    
                    array.append(animations1)
                } else {
                    array.append(obj)
                }
            }
            
            sequence.removeAll()
            
            for obj in array {
                sequence.append(obj)
            }
            
            return sequence
        }
    }
    
    private actor Semaphore {
        private var count: Int
        private var waiters = [CheckedContinuation<Void, Never>]()
        
        init(value: Int = 0) {
            self.count = value
        }
        
        func wait() async {
            self.count -= 1
            
            if self.count >= 0 {
                return
            }
            
            await withCheckedContinuation {
                self.waiters.append($0)
            }
        }
        
        func signal() {
            self.count += 1
            
            if !self.waiters.isEmpty {
                self.waiters.removeFirst().resume()
            }
        }
    }
}

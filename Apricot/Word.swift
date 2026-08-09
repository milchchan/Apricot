//
//  Word.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation

public struct Word: Identifiable, Hashable, Codable {
    public var id: UUID? = UUID()
    public var name: String
    public var attributes: [String]?
    public var timestamp: Int64
    
    public init(name: String) {
        self.name = name
        self.attributes = []
        self.timestamp = Int64(Date().timeIntervalSince1970)
    }
    
    public init(name: String, attributes: [String]?) {
        self.name = name
        self.attributes = attributes
        self.timestamp = Int64(Date().timeIntervalSince1970)
    }
    
    enum CodingKeys: String, CodingKey {
        case name
        case attributes
        case timestamp
    }
}

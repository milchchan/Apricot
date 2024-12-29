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
    
    public init(name: String) {
        self.name = name
        self.attributes = []
    }
    
    public init(name: String, attributes: [String]?) {
        self.name = name
        self.attributes = attributes
    }
    
    enum CodingKeys: String, CodingKey {
        case name
        case attributes
    }
}

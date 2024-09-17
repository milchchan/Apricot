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
    public var attributes: [String] = []
    
    public init(name: String) {
        self.name = name
    }
    
    enum CodingKeys: String, CodingKey {
        case name
        case attributes
    }
}

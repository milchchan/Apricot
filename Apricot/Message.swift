//
//  Message.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation

public struct Message: Identifiable, Collection {
    public var id: UUID?
    public var speed: Double = 50.0
    public var duration: CFTimeInterval = 5.0
    private var inlines: [(text: String, attributes: [String]?)]
    public var content: String {
        var s = String()
        
        for inline in self.inlines {
            s.append(inline.text)
        }
        
        return s
    }
    public var startIndex: Int {
        return self.inlines.startIndex
    }
    public var endIndex: Int {
        return self.inlines.endIndex
    }
    
    public init(inlines: [(text: String, attributes: [String]?)] = []) {
        self.id = UUID()
        self.inlines = inlines
    }
    
    public init(id: UUID?, inlines: [(text: String, attributes: [String]?)] = []) {
        self.id = id
        self.inlines = inlines
    }
    
    public subscript (position: Int) -> (text: String, attributes: [String]?) {
        self.inlines[position]
    }
    
    public func index(after i: Int) -> Int {
        return self.inlines.index(after: i)
    }
    
    public mutating func append(_ newElement: (text: String, attributes: [String]?)) {
        self.inlines.append(newElement)
    }
    
    @discardableResult
    public mutating func remove(at index: Int) -> Element {
        return self.inlines.remove(at: index)
    }
    
    public mutating func removeAll(keepingCapacity keepCapacity: Bool = false) {
        self.inlines.removeAll(keepingCapacity: keepCapacity)
    }
}

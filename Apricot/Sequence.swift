//
//  Sequence.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation

public class Sequence: Collection {
    public var name: String?
    public var state: String?
    private var segments: [Any?] = []
    public var startIndex: Int {
        return self.segments.startIndex
    }
    public var endIndex: Int {
        return self.segments.endIndex
    }
    
    public init(name: String?, state: String? = nil) {
        self.name = name
        self.state = state
    }
    
    public subscript(position: Int) -> Any? {
        self.segments[position]
    }
    
    public func index(after i: Int) -> Int {
        return self.segments.index(after: i)
    }
    
    public func append(_ newElement: Any?) {
        self.segments.append(newElement)
    }
    
    @discardableResult
    public func remove(at index: Int) -> Element {
        return self.segments.remove(at: index)
    }
    
    public func removeAll(keepingCapacity keepCapacity: Bool = false) {
        self.segments.removeAll(keepingCapacity: keepCapacity)
    }
}

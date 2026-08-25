//
//  Sequence.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation

public struct Sequence: Collection, Identifiable, Sendable {
    public indirect enum Step: Sendable {
        case message(Message)
        case synthesis(Message, String)
        case animations([Animation])
        case sound(Sound)
        case audio(Data)
        case sequence(Sequence)
        case completion
    }

    public let id: UUID
    public var name: String?
    public var state: String?
    private var steps: [Step] = []
    public var startIndex: Int {
        return self.steps.startIndex
    }
    public var endIndex: Int {
        return self.steps.endIndex
    }
    
    public init(name: String?, state: String? = nil) {
        self.id = UUID()
        self.name = name
        self.state = state
    }
    
    public subscript(position: Int) -> Step {
        self.steps[position]
    }
    
    public func index(after i: Int) -> Int {
        return self.steps.index(after: i)
    }
    
    public mutating func append(_ newElement: Step) {
        self.steps.append(newElement)
    }
    
    @discardableResult
    public mutating func remove(at index: Int) -> Element {
        return self.steps.remove(at: index)
    }
    
    public mutating func removeAll(keepingCapacity keepCapacity: Bool = false) {
        self.steps.removeAll(keepingCapacity: keepCapacity)
    }
}

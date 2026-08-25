//
//  Animation.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation

public struct Animation: Collection, Sendable {
    public var repeats: UInt = 1
    public var z: Int = 0
    public var type: String? = nil
    private var frames: [Sprite]
    public var startIndex: Int {
        return self.frames.startIndex
    }
    public var endIndex: Int {
        return self.frames.endIndex
    }

    public init(frames: [Sprite] = []) {
        self.frames = frames
    }

    public subscript (position: Int) -> Sprite {
        self.frames[position]
    }

    public func index(after i: Int) -> Int {
        return self.frames.index(after: i)
    }

    public mutating func append(_ newElement: Sprite) {
        self.frames.append(newElement)
    }

    @discardableResult
    public mutating func remove(at index: Int) -> Element {
        return self.frames.remove(at: index)
    }

    public mutating func removeAll(keepingCapacity keepCapacity: Bool = false) {
        self.frames.removeAll(keepingCapacity: keepCapacity)
    }
}

//
//  Animation.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation

public class Animation: Collection, Hashable {
    public var repeats: UInt = 1
    public var z: Int = 0
    public var type: String? = nil
    public var time: Double = 0.0
    private var frames: [Sprite]
    public var duration: Double {
        var duration = 0.0
        
        for frame in self.frames {
            duration += frame.delay
        }
        
        if self.repeats > 1 {
            duration *= Double(self.repeats)
        }
        
        return duration
    }
    public var current: Sprite {
        var time = self.time
        var frame = self.frames[0]
        
        if self.repeats != 1 {
            var duration = 0.0
            
            for frame in self.frames {
                duration += frame.delay
            }
            
            if self.repeats > 1 && time > duration * Double(self.repeats) {
                time = duration
            } else {
                time = time.truncatingRemainder(dividingBy: duration)
            }
        }
        
        if time >= frame.delay {
            var delay = frame.delay
            
            for i in 1..<self.frames.count {
                frame = self.frames[i]
                delay += frame.delay
                
                if time < delay {
                    break
                }
            }
        }
        
        return frame
    }
    public var startIndex: Int {
        return self.frames.startIndex
    }
    public var endIndex: Int {
        return self.frames.endIndex
    }
    
    public static func == (lhs: Animation, rhs: Animation) -> Bool {
        return lhs === rhs
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
    
    public func append(_ newElement: Sprite) {
        self.frames.append(newElement)
    }
    
    @discardableResult
    public func remove(at index: Int) -> Element {
        return self.frames.remove(at: index)
    }
    
    public func removeAll(keepingCapacity keepCapacity: Bool = false) {
        self.frames.removeAll(keepingCapacity: keepCapacity)
    }
    
    public func hash(into hasher: inout Hasher) {
        hasher.combine(ObjectIdentifier(self))
    }
}

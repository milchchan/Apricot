//
//  Timeline.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation

@MainActor
public final class Timeline: @MainActor Hashable {
    public var animation: Animation
    public var time: Double
    public var duration: Double {
        var duration = 0.0

        for frame in self.animation {
            duration += frame.delay
        }

        if self.animation.repeats > 1 {
            duration *= Double(self.animation.repeats)
        }

        return duration
    }
    public var current: Sprite {
        var time = self.time
        var frame = self.animation[self.animation.startIndex]

        if self.animation.repeats != 1 {
            var duration = 0.0

            for frame in self.animation {
                duration += frame.delay
            }

            if self.animation.repeats > 1 && time > duration * Double(self.animation.repeats) {
                time = duration
            } else {
                time = time.truncatingRemainder(dividingBy: duration)
            }
        }

        if time >= frame.delay {
            var delay = frame.delay

            for i in self.animation.index(after: self.animation.startIndex)..<self.animation.endIndex {
                frame = self.animation[i]
                delay += frame.delay

                if time < delay {
                    break
                }
            }
        }

        return frame
    }

    public init(animation: Animation, time: Double = 0.0) {
        self.animation = animation
        self.time = time
    }

    public static func == (lhs: Timeline, rhs: Timeline) -> Bool {
        lhs === rhs
    }

    public func hash(into hasher: inout Hasher) {
        hasher.combine(ObjectIdentifier(self))
    }
}

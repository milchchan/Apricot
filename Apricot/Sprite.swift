//
//  Sprite.swift
//  Apricot
//
//  Created by Milch on 2023/07/01.
//

import Foundation

public struct Sprite: Equatable {
    public var path: String? = nil
    public var location: CGPoint = CGPoint.zero
    public var size: CGSize = CGSize.zero
    public var opacity: Double = 1.0
    public var delay: Double = 0.0
    
    public static func == (lhs: Sprite, rhs: Sprite) -> Bool {
        return lhs.path == rhs.path && lhs.location == rhs.location && lhs.size == rhs.size && lhs.opacity == rhs.opacity && lhs.delay == rhs.delay
    }
}

//
//  KMeans.swift
//  Apricot
//
//  Created by Milch on 2023/04/01.
//

import Foundation

public class KMeans {
    private var numberOfClusters: UInt
    private var centers: [Int: [Double]] = [:]
    var clusters: [Int: [Double]] {
        return self.centers
    }
    
    public init(numberOfClusters: UInt) {
        self.numberOfClusters = numberOfClusters
    }
    
    public func fit(data: [[Double]], iterations: Int = 1000, distance: ([Double], [Double]) -> Double = { x, y in
        // Euclidean distance
        var distance = 0.0
        
        for i in 0..<x.count {
            distance += (x[i] - y[i]) * (x[i] - y[i])
        }
        
        return sqrt(distance)
    }) {
        // k-means++
        var clusters = [Int]()
        var centerVector = data[Int.random(in: 0..<data.count)]
        let epsilon = powl(10, -8)
        var t = 0
        
        self.centers.removeAll()
        self.centers[0] = centerVector
        
        for _ in 0..<data.count {
            clusters.append(0)
        }
        
        for i in 1..<Int(self.numberOfClusters) {
            var probabilities = [Double]()
            var sum = epsilon
            
            for vector in data {
                let distance = distance(centerVector, vector) + epsilon
                
                probabilities.append(distance)
                sum += distance
            }
            
            for j in 0..<probabilities.count {
                probabilities[j] /= sum
            }
            
            centerVector = data[min(self.choice(probabilities: probabilities), probabilities.count - 1)]
            self.centers[i] = centerVector
        }
        
        while t < iterations {
            // Assignment step
            for i in 0..<data.count {
                var minDistance = Double.greatestFiniteMagnitude
                var assignedClusterId = -1
                
                for (key, value) in self.centers {
                    let distance = distance(value, data[i])
                    
                    if distance < minDistance {
                        minDistance = distance
                        assignedClusterId = key
                    }
                }
                
                clusters[i] = assignedClusterId
            }
            
            // Update step
            for i in 0..<self.centers.count {
                var vectors = [[Double]]()
                
                for j in 0..<clusters.count {
                    if clusters[j] == i {
                        vectors.append(data[j])
                    }
                }
                
                if vectors.count > 0 {
                    self.centers[i] = self.mean(x: vectors)
                }
            }
            
            t += 1
        }
    }
    
    public func predict(vector: [Double]) -> (Int, [Double]) {
        var maxSimilarity = 0.0
        var predictedClusterId = 0
        
        for (key, value) in self.centers {
            let similarity = self.cosineSimilarity(x: value, y: vector)
            
            if similarity > maxSimilarity {
                maxSimilarity = similarity
                predictedClusterId = key
            }
        }
        
        return (predictedClusterId, self.centers[predictedClusterId]!)
    }
    
    private func cosineSimilarity(x: [Double], y: [Double]) -> Double {
        let epsilon: Double = pow(10, -8)
        var sum = 0.0
        var normX = 0.0
        var normY = 0.0
        
        for i in 0..<x.count {
            sum += x[i] * y[i]
            normX += x[i] * x[i]
            normY += y[i] * y[i]
        }
        
        return (sum + epsilon) / (sqrt(normX) * sqrt(normY) + epsilon)
    }
    
    private func choice(probabilities: [Double]) -> Int {
        let random = Double.random(in: 0.0..<1.0)
        var sum = 0.0
        var index = 0
        
        for probability in probabilities {
            if sum <= random && random < sum + probability {
                break
            }
            
            sum += probability
            index += 1
        }
        
        return index
    }
    
    private func mean(x: [[Double]]) -> [Double] {
        var vector = [Double]()
        
        for i in 0..<x[0].count {
            vector.append(x[0][i])
        }
        
        for i in 1..<x.count {
            for j in 0..<vector.count {
                vector[j] += x[i][j]
            }
        }
        
        for i in 0..<vector.count {
            vector[i] = vector[i] / Double(x.count)
        }
        
        return vector
    }
}

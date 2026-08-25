//
//  KMeans.swift
//  Apricot
//
//  Created by Milch on 2023/04/01.
//

import Foundation

public class KMeans {
    private var numberOfClusters: Int
    private var centers: [Int: [Double]] = [:]
    var clusters: [Int: [Double]] {
        return self.centers
    }
    
    public init(numberOfClusters: Int) {
        self.numberOfClusters = numberOfClusters
    }
    
    public func fit(data: [[Double]], iterations: Int = 1000) {
        // k-means++
        var clusters = [Int]()
        var centerVector = data[Int.random(in: 0..<data.count)]
        var t = 0
        
        self.centers.removeAll()
        self.centers[0] = centerVector
        
        for _ in 0..<data.count {
            clusters.append(0)
        }
        
        for i in 1..<self.numberOfClusters {
            var probabilities = [Double]()
            var sum = 0.0
            
            for vector in data {
                var minDistance = Double.greatestFiniteMagnitude
                
                for center in self.centers.values {
                    minDistance = min(minDistance, self.euclideanDistance(x: center, y: vector))
                }
                
                let squaredDistance = minDistance * minDistance
                
                probabilities.append(squaredDistance)
                sum += squaredDistance
            }
            
            if (sum == 0.0) {
                break
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
                    let distance = self.euclideanDistance(x: value, y: data[i])
                    
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
        var minDistance = Double.greatestFiniteMagnitude
        var predictedClusterId = 0
        
        for (key, value) in self.centers {
            let distance = self.euclideanDistance(x: value, y: vector)
            
            if distance < minDistance {
                minDistance = distance
                predictedClusterId = key
            }
        }
        
        return (predictedClusterId, self.centers[predictedClusterId]!)
    }
    
    private nonisolated func euclideanDistance(x: [Double], y: [Double]) -> Double {
        var distance = 0.0
        
        for i in 0..<x.count {
            distance += (x[i] - y[i]) * (x[i] - y[i])
        }
        
        return sqrt(distance)
    }
    
    private nonisolated func choice(probabilities: [Double]) -> Int {
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
    
    private nonisolated func mean(x: [[Double]]) -> [Double] {
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

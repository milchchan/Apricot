//
//  ApricotApp.swift
//  Apricot
//
//  Created by Milch on 2023/01/01.
//

import SwiftUI
import BackgroundTasks
import CryptoKit
import StoreKit
import UIKit

@main
struct ApricotApp: App {
    @UIApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    @Environment(\.scenePhase) private var scenePhase
    
    var body: some Scene {
        WindowGroup {
            Chat()
        }
        .onChange(of: self.scenePhase) {
            if self.scenePhase == .background {
                UIApplication.shared.shortcutItems = [UIApplicationShortcutItem(type: "Dictionary", localizedTitle: String(localized: "Dictionary"), localizedSubtitle: nil, icon: UIApplicationShortcutIcon.init(systemImageName: "book"))]
                
                let request = BGAppRefreshTaskRequest(identifier: "com.milchchan.Apricot.refresh")
                
                request.earliestBeginDate = Calendar.current.date(byAdding: .hour, value: 1, to: Date())!
                try? BGTaskScheduler.shared.submit(request)
            }
        }
        .backgroundTask(.appRefresh("com.milchchan.Apricot.refresh")) {
            let taskRequest = BGAppRefreshTaskRequest(identifier: "com.milchchan.Apricot.refresh")
            
            taskRequest.earliestBeginDate = Calendar.current.date(byAdding: .hour, value: 1, to: Date())!
            try? BGTaskScheduler.shared.submit(taskRequest)
            
            let config = URLSessionConfiguration.background(withIdentifier: "com.milchchan.Apricot.refresh")
            
            config.sessionSendsLaunchEvents = true
            
            let url = URL(string: "https://milchchan.com/api/likes")!
            let session = URLSession(configuration: config)
            let request = URLRequest(url: url)
            if let (data, response) = await withTaskCancellationHandler(operation: {
                try? await session.data(for: request)
            }, onCancel: {
                let task = session.downloadTask(with: request)
                
                task.resume()
            }), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "application/json" {
                await Task.detached {
                    if let cachesUrl = FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask).first {
                        let path = cachesUrl.appending(path: SHA256.hash(data: Data(url.absoluteString.utf8)).compactMap { String(format: "%02x", $0) }.joined(), directoryHint: .inferFromPath).path(percentEncoded: false)
                        
                        if FileManager.default.fileExists(atPath: path) {
                            if let file = FileHandle(forWritingAtPath: path) {
                                defer {
                                    try? file.close()
                                }
                                
                                try? file.truncate(atOffset: 0)
                                try? file.write(contentsOf: data)
                            }
                        } else {
                            FileManager.default.createFile(atPath: path, contents: data, attributes: nil)
                        }
                    }
                }.value
            }
        }
    }
}

class AppDelegate: NSObject, UIApplicationDelegate {
    private var updateListenerTask: Task<Void, Error>? = nil
    
    func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey : Any]? = nil) -> Bool {
        BGTaskScheduler.shared.cancelAllTaskRequests()
        
        self.updateListenerTask = Task.detached(priority: .background) {
            for await verificationResult in Transaction.updates {
                guard case .verified(let transaction) = verificationResult, transaction.revocationDate == nil else {
                    return
                }
                
                await transaction.finish()
            }
        }
        
        return true
    }
    
    func application(_ application: UIApplication, configurationForConnecting connectingSceneSession: UISceneSession, options: UIScene.ConnectionOptions) -> UISceneConfiguration {
        if let shortcutItem = options.shortcutItem {
            Shortcut.shared.type = [shortcutItem.type]
        }
        
        let configuration = UISceneConfiguration(name: connectingSceneSession.configuration.name, sessionRole: connectingSceneSession.role)

        configuration.delegateClass = SceneDelegate.self
        
        return configuration
    }
    
    func applicationWillTerminate(_ application: UIApplication) {
        self.updateListenerTask?.cancel()
    }
}

class SceneDelegate: UIResponder, UIWindowSceneDelegate {
    func windowScene(_ windowScene: UIWindowScene, performActionFor shortcutItem: UIApplicationShortcutItem, completionHandler: @escaping (Bool) -> Void) {
        Shortcut.shared.type = [shortcutItem.type]
        
        completionHandler(true)
    }
    
    func scene(_ scene: UIScene, willConnectTo session: UISceneSession, options connectionOptions: UIScene.ConnectionOptions) {
        guard let _ = scene as? UIWindowScene else {
            return
        }
        
        if let shortcutItem = connectionOptions.shortcutItem {
            Shortcut.shared.type = [shortcutItem.type]
        }
    }
}

final class Shortcut: ObservableObject {
    static let shared = Shortcut()
    @Published var type: [String]?
    
    private init() {
        self.type = nil
    }
}

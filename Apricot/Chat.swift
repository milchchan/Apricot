//
//  Chat.swift
//  Apricot
//
//  Created by Milch on 2023/01/01.
//

import SwiftUI
import Charts
import AppIntents
import UniformTypeIdentifiers
import AVFoundation
import Synchronization
import Speech
import Vision
import CryptoKit
import StoreKit
import UIKit

struct Chat: View {
   @Environment(\.scenePhase) private var scenePhase
   @Environment(\.horizontalSizeClass) private var horizontalSizeClass
   @Environment(\.verticalSizeClass) private var verticalSizeClass
   @Environment(\.openURL) private var openURL
   @FocusState private var composerFocused: Bool
   @Namespace private var menuNamespace
   @StateObject private var shortcut = Shortcut.shared
   @StateObject private var script: Script
   @State private var prompt: (String?, Word?, Bool, Set<Character>?, [(String, URL?)], Int, Double) = (nil, nil, false, nil, [], 0, 0)
   @State private var logs = [(id: UUID?, from: String?, to: String?, group: Double, raw: String?, content: (text: String?, image: CGImage?), choices: [String]?)]()
   @State private var labels = [String]()
   @State private var likability: Double? = nil
   @State private var choices = [(String, URL?)]()
   @State private var discoveries = [Word]()
   @State private var revealMenu = false
   @State private var showActivity = false
   @State private var showDictionary = false
   @State private var showGallery = false
   @State private var showSettings = false
   @State private var showComposer = false
   @State private var selection: String
   @State private var isActive = false
   @State private var isRecording = false
   @State private var isPeeking = false
   @State private var isPeekable = true
   @State private var isPaused = false
   @State private var isChanging = false
   @State private var isIdle = false
   @State private var isLoading = false
   @State private var loadingAmount = 0.0
   @State private var shakes = 0
   @State private var volumeLevel = 0.0
   @State private var audioEngine: AVAudioEngine? = nil
   @State private var speechRecognizer: SFSpeechRecognizer? = nil
   @State private var speechAudioBufferRecognitionRequest: SFSpeechAudioBufferRecognitionRequest? = nil
   @State private var speechRecognitionTask: SFSpeechRecognitionTask? = nil
   @State private var message = String()
   private var path: AppStorage<String>
   private var accent: AppStorage<String>
   @AppStorage("types") private var types = 0
   @AppStorage("scale") private var scale = 1.0
   @AppStorage("mute") private var mute = false
   @AppStorage("temperature") private var temperature = 1.0
   @AppStorage("energy") private var energy = 0.0
   @AppStorage("timestamp") private var timestamp = 0
   private let maxEnergy = 10.0
   private let recoveryDuration = 3600.0
   private let starImage: UIImage
   
   var body: some View {
      GeometryReader { geometry in
         ZStack {
            ZStack {
               VStack {
                  Stage(prompt: self.$prompt, logs: self.$logs, resource: Binding<(old: String, new: String)>(get: { (old: self.selection, new: self.path.wrappedValue) }, set: { newValue in
                     self.selection = newValue.old
                     self.path.wrappedValue = newValue.new
                  }), attributes: self.$script.attributes, types: self.$types, labels: self.$labels, likability: self.$likability, choices: self.$choices, words: self.$script.words, active: self.isActive, pause: self.revealMenu || self.showActivity || self.showDictionary || self.showGallery || self.showSettings, idle: self.$isIdle, changing: self.$isChanging, loading: self.$isLoading, discoveries: self.$discoveries, temperature: self.temperature, accent: self.convert(from: self.accent.wrappedValue), scale: self.scale, mute: self.mute)
                  .frame(
                     minWidth: 0.0,
                     maxWidth: .infinity,
                     minHeight: 0.0,
                     maxHeight: .infinity
                  )
                  .background(.clear)
                  .ignoresSafeArea(.all)
               }
               Color.clear
                  .contentShape(Rectangle())
                  .allowsHitTesting(self.showComposer)
                  .ignoresSafeArea(.all)
                  .onTapGesture {
                     self.composerFocused = false
                     
                     Task {
                        await Task.detached {
                           await MainActor.run {
                              withAnimation {
                                 self.showComposer = false
                              }
                           }
                        }.value
                     }
                  }
               VStack(spacing: 0.0) {
                  VStack(spacing: 16.0) {
                     ZStack(alignment: .bottom) {
                        Rectangle()
                           .frame(
                              width: self.starImage.size.width,
                              height: self.starImage.size.height,
                              alignment: .top
                           )
                           .background(.clear)
                           .foregroundStyle(.clear)
                        Image(uiImage: self.starImage.withRenderingMode(.alwaysTemplate))
                           .frame(
                              width: self.starImage.size.width,
                              height: self.starImage.size.height - round(self.starImage.size.height * (self.likability ?? 0.0)),
                              alignment: .top
                           )
                           .background(.clear)
                           .foregroundStyle(.primary)
                           .clipped()
                           .offset(y: -round(self.starImage.size.height * (self.likability ?? 0.0)))
                        Image(uiImage: self.starImage.withRenderingMode(.alwaysTemplate))
                           .frame(
                              width: self.starImage.size.width,
                              height: round(self.starImage.size.height * (self.likability ?? 0.0)),
                              alignment: .bottom
                           )
                           .background(.clear)
                           .foregroundStyle(Color(self.convert(from: self.accent.wrappedValue)))
                           .clipped()
                     }
                     .padding(EdgeInsets(
                        top: 8.0,
                        leading: 0.0,
                        bottom: 0.0,
                        trailing: 0.0
                     ))
                     Text(String(format: "%ld", self.script.words.count))
                        .frame(
                           alignment: .top
                        )
                        .offset(y: -floor(UIFont(name: "DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0))!.ascender - UIFont(name: "DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0))!.capHeight))
                        .background(.clear)
                        .foregroundStyle(.primary)
                        .font(.custom("DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0)))
                        .lineLimit(1)
                        .truncationMode(.tail)
                        .contentTransition(.numericText(value: Double(self.script.words.count)))
                     
                     if self.isPeeking {
                        ZStack {
                           ZStack {
                              Peek(peekable: self.$isPeekable, ready: self.isActive && self.isIdle && !self.isLoading && !self.revealMenu && !self.showActivity && !self.showDictionary && !self.showGallery && !self.showSettings, pause: self.isPaused, onChange: { image in
                                 if self.energy >= 1.0 {
                                    self.choices.removeAll()
                                    self.timestamp = Int(Date().timeIntervalSince1970)
                                    
                                    withAnimation {
                                       self.energy -= 1.0
                                    }
                                    
                                    Task {
                                       await self.talk(image: image, temperature: self.temperature, multiple: geometry.size.width > geometry.size.height, mute: self.mute)
                                    }
                                 } else {
                                    self.shakes += 1
                                 }
                              })
                              .frame(
                                 maxWidth: .infinity,
                                 maxHeight: .infinity
                              )
                              .background(.clear)
                              
                              if !self.isPeekable {
                                 Image(systemName: "exclamationmark.triangle")
                                    .symbolRenderingMode(.monochrome)
                                    .frame(
                                       width: 16.0,
                                       height: 16.0,
                                       alignment: .center
                                    )
                                    .background(.clear)
                                    .foregroundStyle(Color(hue: 0.0, saturation: 0.0, brightness: 1.0, opacity: 1.0))
                                    .font(
                                       .system(size: 16.0)
                                    )
                                    .bold()
                              } else if self.isPaused {
                                 Image(systemName: "pause")
                                    .frame(
                                       width: 16.0,
                                       height: 16.0,
                                       alignment: .center
                                    )
                                    .background(.clear)
                                    .foregroundStyle(Color(hue: 0.0, saturation: 0.0, brightness: 1.0, opacity: 1.0))
                                    .font(.system(size: 16.0))
                                    .bold()
                              }
                           }
                           .frame(
                              width: self.horizontalSizeClass == .compact && self.verticalSizeClass == .regular && geometry.size.width < geometry.size.height ? (geometry.size.width - 32.0) / 2.0 : (geometry.size.width / 2.0 - 32.0) / 2.0,
                              height: self.horizontalSizeClass == .compact && self.verticalSizeClass == .regular && geometry.size.width < geometry.size.height ? (geometry.size.width - 32.0) / 2.0 : (geometry.size.width / 2.0 - 32.0) / 2.0,
                              alignment: .top
                           )
                           .background(Color(hue: 0.0, saturation: 0.0, brightness: 0.0, opacity: 1.0))
                           .overlay {
                              if !self.revealMenu {
                                 GeometryReader { proxy in
                                    ZStack(alignment: .bottom) {
                                       Rectangle()
                                          .fill(Color(self.convert(from: self.accent.wrappedValue)).opacity(0.25))
                                          .frame(
                                             width: proxy.size.width + 32.0,
                                             height: proxy.size.height
                                          )
                                       Rectangle()
                                          .fill(Color(self.convert(from: self.accent.wrappedValue)))
                                          .frame(
                                             width: proxy.size.width + 32.0,
                                             height: proxy.size.height * CGFloat(min(max(self.energy / self.maxEnergy, 0.0), 1.0))
                                          )
                                    }
                                    .frame(
                                       width: proxy.size.width + 32.0,
                                       height: proxy.size.height,
                                       alignment: .bottom
                                    )
                                    .mask {
                                       Path { path in
                                          path.addRect(CGRect(x: 0.0, y: 0.0, width: proxy.size.width + 32.0, height: proxy.size.height))
                                          path.addRoundedRect(in: CGRect(x: 20.0, y: 4.0, width: proxy.size.width - 8.0, height: proxy.size.height - 8.0), cornerSize: CGSize(width: 12.0, height: 12.0))
                                       }
                                       .fill(Color.white, style: FillStyle(eoFill: true))
                                    }
                                    .frame(
                                       width: proxy.size.width,
                                       height: proxy.size.height,
                                       alignment: .center
                                    )
                                 }
                                 .allowsHitTesting(false)
                                 .animation(.easeInOut(duration: 0.5), value: self.energy)
                                 .keyframeAnimator(initialValue: 0, trigger: self.shakes, content: { view, value in
                                    view
                                       .offset(x: value)
                                 }, keyframes: { _ in
                                    MoveKeyframe(5.0)
                                    LinearKeyframe(5.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(0.0)
                                    LinearKeyframe(0.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(-5.0)
                                    LinearKeyframe(-5.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(4.0)
                                    LinearKeyframe(4.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(0.0)
                                    LinearKeyframe(0.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(-4.0)
                                    LinearKeyframe(-4.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(3.0)
                                    LinearKeyframe(3.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(0.0)
                                    LinearKeyframe(0.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(-3.0)
                                    LinearKeyframe(-3.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(2.0)
                                    LinearKeyframe(2.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(0.0)
                                    LinearKeyframe(0.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(-2.0)
                                    LinearKeyframe(-2.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(1.0)
                                    LinearKeyframe(1.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(0.0)
                                    LinearKeyframe(0.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(-1.0)
                                    LinearKeyframe(-1.0, duration: 0.5 / 15.0)
                                    MoveKeyframe(0.0)
                                 })
                              }
                           }
                           .clipShape(RoundedRectangle(cornerRadius: 16.0))
                           .transition(.opacity)
                           .onLongPressGesture(perform: {
                              withAnimation(.linear(duration: 0.5)) {
                                 self.isPaused.toggle()
                              }
                           })
                        }
                        .offset(y: -floor(UIFont(name: "DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0))!.lineHeight - UIFont(name: "DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0))!.capHeight))
                     }
                     
                     if self.isLoading {
                        HStack(spacing: 8.0) {
                           ForEach(0..<3) { index in
                              Circle()
                                 .frame(width: 8, height: 8)
                                 .foregroundStyle(.primary)
                                 .scaleEffect(0.5 + self.loadingAmount * 0.5)
                                 .opacity(0.5 + self.loadingAmount * 0.5)
                                 .animation(.easeInOut(duration: 1.0).repeatForever(autoreverses: true).delay(Double(index) * 0.5), value: self.loadingAmount)
                           }
                        }
                        .offset(y: -floor(UIFont(name: "DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0))!.lineHeight - UIFont(name: "DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0))!.capHeight))
                        .transition(.opacity)
                        .onAppear {
                           self.loadingAmount = 1.0
                        }
                        .onDisappear {
                           self.loadingAmount = 0.0
                        }
                     }
                  }
                  Spacer()
               }
               .frame(
                  alignment: .top
               )
               .padding(0.0)
               .background(.clear)
               VStack(spacing: 0.0) {
                  Spacer()
                     .frame(
                        minHeight: 0.0
                     )
                  ZStack(alignment: .bottom) {
                     if self.revealMenu {
                        self.makeMenu(geometryProxy: geometry)
                     }
                     
                     if !self.showComposer {
                        VStack(spacing: 16.0) {
                           GlassEffectContainer(spacing: 16.0) {
                              HStack(spacing: 16.0) {
                                 if self.revealMenu {
                                    Button(action: {
                                       withAnimation {
                                          self.isPeeking.toggle()
                                          self.revealMenu = false
                                       }
                                    }) {
                                       ZStack {
                                          Image(systemName: "camera.aperture")
                                             .frame(
                                                width: 48.0,
                                                height: 48.0,
                                                alignment: .center
                                             )
                                             .background(.clear)
                                             .foregroundStyle(.primary)
                                             .font(
                                                .system(size: 16.0)
                                             )
                                             .bold()
                                          Circle()
                                             .frame(width: 4, height: 4)
                                             .foregroundStyle(Color(self.convert(from: self.accent.wrappedValue)))
                                             .opacity(self.isPeeking ? 1.0 : 0.0)
                                             .transition(.opacity)
                                             .frame(
                                                maxWidth: .infinity,
                                                maxHeight: .infinity,
                                                alignment: .topTrailing
                                             )
                                             .offset(x: -12.0, y: 12.0)
                                       }
                                       .frame(
                                          width: 48.0,
                                          height: 48.0
                                       )
                                    }
                                    .frame(
                                       alignment: .center
                                    )
                                    .foregroundStyle(.primary)
                                    .glassEffect(.regular.interactive(), in: Circle())
                                    .glassEffectID("vision", in: self.menuNamespace)
                                    .glassEffectTransition(.matchedGeometry)
                                    .clipShape(Circle())
                                 }
                                 
                                 Button(action: {
                                    if self.revealMenu {
                                       withAnimation {
                                          self.revealMenu.toggle()
                                       }
                                    } else {
                                       let now = Int(Date().timeIntervalSince1970)
                                       let elapsed = now - self.timestamp
                                       
                                       if self.script.words.isEmpty {
                                          self.prompt = (nil, nil, false, nil, self.choices, 0, CACurrentMediaTime())
                                       } else {
                                          let samples = 10
                                          var letterSet = Set<Character>()
                                          var modifiers = [String]()
                                          var words = [Word]()
                                          
                                          for _ in 0..<samples {
                                             let word = self.script.words[Int.random(in: 0..<self.script.words.count)]
                                             
                                             for i in 0..<word.name.count {
                                                let character = word.name[word.name.index(word.name.startIndex, offsetBy: i)]
                                                
                                                if !letterSet.contains(character) && !character.isNewline && !character.isWhitespace {
                                                   letterSet.insert(character)
                                                }
                                             }
                                             
                                             if let attributes = word.attributes, attributes.isEmpty {
                                                modifiers.append(word.name)
                                             } else {
                                                words.append(word)
                                             }
                                          }
                                          
                                          if words.isEmpty {
                                             self.prompt = (nil, nil, false, nil, self.choices, 0, CACurrentMediaTime())
                                          } else {
                                             var word = words[Int.random(in: 0..<words.count)]
                                             
                                             if Double.random(in: 0.0..<1.0) < Double(modifiers.count) / Double(samples) {
                                                let modifier = modifiers[Int.random(in: 0..<modifiers.count)]
                                                
                                                if modifier.allSatisfy({ $0.isASCII }) && word.name.allSatisfy({ $0.isASCII }) {
                                                   word.name = modifier + String("\u{0020}\u{000A}") + word.name
                                                } else {
                                                   word.name = modifier + "\n" + word.name
                                                }
                                             }
                                             
                                             self.prompt = (word.name, word, true, letterSet, self.choices, 0, CACurrentMediaTime())
                                          }
                                       }
                                       
                                       self.timestamp = now
                                       
                                       withAnimation {
                                          self.energy = min(self.energy + Double(max(elapsed, 0)) / self.recoveryDuration * self.maxEnergy, self.maxEnergy)
                                          self.revealMenu.toggle()
                                       }
                                    }
                                 }) {
                                    if self.revealMenu {
                                       Image(systemName: "chevron.down")
                                          .frame(
                                             width: 48.0,
                                             height: 48.0,
                                             alignment: .center
                                          )
                                          .background(.clear)
                                          .foregroundStyle(.primary)
                                          .font(
                                             .system(size: 16.0)
                                          )
                                          .bold()
                                          .padding(0.0)
                                          .transition(.opacity)
                                    } else {
                                       Image(systemName: "chevron.up")
                                          .frame(
                                             width: 48.0,
                                             height: 48.0,
                                             alignment: .center
                                          )
                                          .background(.clear)
                                          .foregroundStyle(.primary)
                                          .font(
                                             .system(size: 16.0)
                                          )
                                          .bold()
                                          .padding(0.0)
                                          .transition(.opacity)
                                    }
                                 }
                                 .frame(
                                    alignment: .center
                                 )
                                 .foregroundStyle(.primary)
                                 .glassEffect(.regular.interactive(), in: Circle())
                                 .glassEffectID("menu", in: self.menuNamespace)
                                 .glassEffectTransition(.matchedGeometry)
                                 .clipShape(Circle())
                                 
                                 if self.revealMenu {
                                    Button(action: {
                                       self.composerFocused = true
                                       
                                       withAnimation {
                                          self.showComposer = true
                                          self.revealMenu = false
                                       }
                                    }) {
                                       Image(systemName: "keyboard")
                                          .frame(
                                             width: 48.0,
                                             height: 48.0,
                                             alignment: .center
                                          )
                                          .background(.clear)
                                          .foregroundStyle(.primary)
                                          .font(
                                             .system(size: 16.0)
                                          )
                                          .bold()
                                    }
                                    .frame(
                                       alignment: .center
                                    )
                                    .foregroundStyle(.primary)
                                    .glassEffect(.regular.interactive(), in: Circle())
                                    .glassEffectID("keyboard", in: self.menuNamespace)
                                    .glassEffectTransition(.matchedGeometry)
                                    .clipShape(Circle())
                                 }
                              }
                              .compositingGroup()
                              .shadow(color: Color(hue: 0.0, saturation: 0.0, brightness: 0.0, opacity: 0.25), radius: 8.0, x: 0.0, y: 0.0)
                           }
                        }
                        .padding(EdgeInsets(
                           top: 0.0,
                           leading: 16.0,
                           bottom: geometry.safeAreaInsets.bottom + 8.0,
                           trailing: 16.0
                        ))
                        .transition(.opacity)
                     }
                  }
                  .background(.clear)
                  .sheet(isPresented: self.$showActivity, content: {
                     Activity(accent: self.convert(from: self.accent.wrappedValue), words: self.$script.words, scores: Script.shared.scores, characters: Script.shared.characters.reduce(into: [], { x, y in
                        if !y.guest {
                           var sequences = [Sequence]()
                           
                           for sequence in y.sequences {
                              if sequence.name == "Star" {
                                 sequences.append(sequence)
                              }
                           }
                           
                           x.append((name: y.name, language: y.language, sequences: sequences))
                        }
                     }), logs: self.$logs)
                     .presentationBackground(.ultraThinMaterial)
                     .presentationDetents([.large])
                  })
                  .sheet(isPresented: self.$showDictionary, content: {
                     Dictionary(active: self.isActive, accent: self.convert(from: self.accent.wrappedValue), type: Binding<String?>(get: {
                        if let type = self.shortcut.type, !type.isEmpty && type[0] == "Dictionary" {
                           if type.count == 1 {
                              return String()
                           } else {
                              return type[1]
                           }
                        }
                        
                        return nil
                     }, set: { newValue in
                        if newValue == nil {
                           self.shortcut.type = nil
                        }
                     }), words: self.$script.words, attributes: self.script.attributes)
                     .presentationBackground(.ultraThinMaterial)
                     .presentationDetents([.medium])
                  })
                  .sheet(isPresented: self.$showGallery, content: {
                     Gallery(accent: self.convert(from: self.accent.wrappedValue))
                        .presentationBackground(.ultraThinMaterial)
                        .presentationDetents([.large])
                  })
                  .sheet(isPresented: self.$showSettings, content: {
                     Settings(resource: Binding<String>(get: { self.path.wrappedValue }, set: { newValue in
                        self.path.wrappedValue = newValue
                     }), changing: self.$isChanging, temperature: self.$temperature, accent: Binding<UIColor>(get: {
                        return self.convert(from: self.accent.wrappedValue)
                     }, set: { color in
                        let red: CGFloat
                        let green: CGFloat
                        let blue: CGFloat
                        
                        if let components = color.cgColor.components {
                           let index = color.cgColor.numberOfComponents - 2
                           
                           red = components[min(0, index)]
                           green = components[min(1, index)]
                           blue = components[min(2, index)]
                        } else {
                           red = 0.0
                           green = 0.0
                           blue = 0.0
                        }
                        
                        self.accent.wrappedValue = String.init(format: "#%02lx%02lx%02lx", lroundf(Float(red * 255)), lroundf(Float(green * 255)), lroundf(Float(blue * 255)))
                     }), scale: self.$scale, mute: self.$mute)
                     .presentationBackground(.ultraThinMaterial)
                     .presentationDetents([.large])
                  })
               }
               .background(.clear)
               .ignoresSafeArea(.all)
            }
            .frame(
               minWidth: 0.0,
               maxWidth: .infinity,
               minHeight: 0.0,
               maxHeight: .infinity,
               alignment: .topLeading
            )
         }
         .safeAreaInset(edge: .bottom) {
            if self.showComposer {
               ZStack(alignment: .bottom) {
                  VStack(spacing: 16.0) {
                     ZStack(alignment: .topLeading) {
                        TextField("Message", text: self.$message, axis: .vertical)
                           .font(.headline)
                           .fontWeight(.semibold)
                           .lineLimit(2)
                           .focused(self.$composerFocused)
                           .textInputAutocapitalization(.never)
                           .disableAutocorrection(true)
                           .background(.clear)
                           .tint(Color(self.convert(from: self.accent.wrappedValue)))
                           .frame(
                              maxWidth: .infinity
                           )
                           .onKeyPress(.return, phases: .down) { press in
                              guard press.modifiers.contains(.command) else {
                                 return .ignored
                              }
                              
                              if self.message.isEmpty {
                                 self.composerFocused = false
                                 
                                 Task {
                                    await Task.detached {
                                       await MainActor.run {
                                          withAnimation {
                                             self.showComposer = false
                                          }
                                       }
                                    }.value
                                 }
                              } else if self.energy >= 1.0 {
                                 self.choices.removeAll()
                                 self.timestamp = Int(Date().timeIntervalSince1970)
                                 
                                 withAnimation {
                                    self.energy -= 1.0
                                 }
                                 
                                 Task {
                                    let message = self.message
                                    
                                    await Task.detached {
                                       await MainActor.run {
                                          self.composerFocused = false
                                          self.message = String()
                                       }
                                       
                                       await MainActor.run {
                                          withAnimation {
                                             self.showComposer = false
                                          }
                                       }
                                    }.value
                                    
                                    await self.talk(word: Word(name: message, attributes: [String](Script.shared.words.reduce(into: Set<String>(), { x, y in
                                       if y.name == message, let attributes = y.attributes {
                                          for attribute in attributes {
                                             if !x.contains(attribute) {
                                                x.insert(attribute)
                                             }
                                          }
                                       }
                                    }))), temperature: self.temperature, multiple: geometry.size.width > geometry.size.height, fallback: false, mute: self.mute)
                                 }
                              } else {
                                 self.shakes += 1
                              }
                              
                              return .handled
                           }
                           .onChange(of: self.composerFocused) {
                              if self.composerFocused && self.isRecording {
                                 self.stopRecognize()
                              }
                           }
                           .onDisappear {
                              if self.isRecording {
                                 self.stopRecognize()
                              }
                           }
                     }
                     .background(.clear)
                     .padding(EdgeInsets(
                        top: 24.0,
                        leading: 24.0,
                        bottom: 0.0,
                        trailing: 24.0
                     ))
                     HStack(spacing: 8.0) {
                        ZStack {
                           Circle()
                              .fill(Color(self.convert(from: self.accent.wrappedValue)).opacity(0.25))
                           Circle()
                              .fill(Color(self.convert(from: self.accent.wrappedValue)))
                              .mask {
                                 Circle()
                                    .trim(from: 0.0, to: CGFloat(min(max(self.energy / self.maxEnergy, 0.0), 1.0)))
                                    .stroke(.white, style: StrokeStyle(lineWidth: 16.0, lineCap: .butt))
                                    .rotationEffect(.degrees(-90.0))
                              }
                        }
                        .mask {
                           ZStack {
                              Circle()
                                 .fill(.white)
                              Circle()
                                 .inset(by: 2.0)
                                 .fill(.white)
                                 .blendMode(.destinationOut)
                           }
                           .compositingGroup()
                        }
                        .frame(
                           width: 16.0,
                           height: 16.0
                        )
                        .padding(EdgeInsets(
                           top: 0.0,
                           leading: 16.0,
                           bottom: 0.0,
                           trailing: 0.0
                        ))
                        .animation(.easeInOut(duration: 0.5), value: self.energy)
                        .keyframeAnimator(initialValue: 0, trigger: self.shakes, content: { view, value in
                           view
                              .offset(x: value)
                        }, keyframes: { _ in
                           MoveKeyframe(5.0)
                           LinearKeyframe(5.0, duration: 0.5 / 15.0)
                           MoveKeyframe(0.0)
                           LinearKeyframe(0.0, duration: 0.5 / 15.0)
                           MoveKeyframe(-5.0)
                           LinearKeyframe(-5.0, duration: 0.5 / 15.0)
                           MoveKeyframe(4.0)
                           LinearKeyframe(4.0, duration: 0.5 / 15.0)
                           MoveKeyframe(0.0)
                           LinearKeyframe(0.0, duration: 0.5 / 15.0)
                           MoveKeyframe(-4.0)
                           LinearKeyframe(-4.0, duration: 0.5 / 15.0)
                           MoveKeyframe(3.0)
                           LinearKeyframe(3.0, duration: 0.5 / 15.0)
                           MoveKeyframe(0.0)
                           LinearKeyframe(0.0, duration: 0.5 / 15.0)
                           MoveKeyframe(-3.0)
                           LinearKeyframe(-3.0, duration: 0.5 / 15.0)
                           MoveKeyframe(2.0)
                           LinearKeyframe(2.0, duration: 0.5 / 15.0)
                           MoveKeyframe(0.0)
                           LinearKeyframe(0.0, duration: 0.5 / 15.0)
                           MoveKeyframe(-2.0)
                           LinearKeyframe(-2.0, duration: 0.5 / 15.0)
                           MoveKeyframe(1.0)
                           LinearKeyframe(1.0, duration: 0.5 / 15.0)
                           MoveKeyframe(0.0)
                           LinearKeyframe(0.0, duration: 0.5 / 15.0)
                           MoveKeyframe(-1.0)
                           LinearKeyframe(-1.0, duration: 0.5 / 15.0)
                           MoveKeyframe(0.0)
                        })
                        Spacer()
                        Button(action: {
                           if self.isRecording {
                              self.stopRecognize()
                           } else {
                              self.startRecognize()
                           }
                        }) {
                           if self.isRecording {
                              Image(systemName: "mic")
                                 .frame(
                                    width: 48.0,
                                    height: 48.0,
                                    alignment: .center
                                 )
                                 .background(.clear)
                                 .foregroundStyle(Color(self.convert(from: self.accent.wrappedValue)))
                                 .font(
                                    .system(size: 16.0)
                                 )
                                 .bold()
                                 .padding(0.0)
                                 .opacity(0.5 + 0.5 * (1.0 - self.volumeLevel))
                                 .transition(.opacity)
                           } else {
                              Image(systemName: "mic")
                                 .frame(
                                    width: 48.0,
                                    height: 48.0,
                                    alignment: .center
                                 )
                                 .background(.clear)
                                 .foregroundStyle(.primary)
                                 .font(
                                    .system(size: 16.0)
                                 )
                                 .bold()
                                 .padding(0.0)
                                 .transition(.opacity)
                           }
                        }
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .clipShape(Circle())
                        Button(action: {
                           if self.energy >= 1.0 {
                              self.choices.removeAll()
                              self.timestamp = Int(Date().timeIntervalSince1970)
                              
                              withAnimation {
                                 self.energy -= 1.0
                              }
                              
                              Task {
                                 let message = self.message
                                 
                                 await Task.detached {
                                    await MainActor.run {
                                       self.composerFocused = false
                                       self.message = String()
                                    }
                                    
                                    await MainActor.run {
                                       withAnimation {
                                          self.showComposer = false
                                       }
                                    }
                                 }.value
                                 
                                 await self.talk(word: Word(name: message, attributes: [String](Script.shared.words.reduce(into: Set<String>(), { x, y in
                                    if y.name == message, let attributes = y.attributes {
                                       for attribute in attributes {
                                          if !x.contains(attribute) {
                                             x.insert(attribute)
                                          }
                                       }
                                    }
                                 }))), temperature: self.temperature, multiple: geometry.size.width > geometry.size.height, fallback: false, mute: self.mute)
                              }
                           } else {
                              self.shakes += 1
                           }
                        }) {
                           Image(systemName: "arrow.up")
                              .frame(
                                 width: 48.0,
                                 height: 48.0,
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(Color(self.convert(from: self.accent.wrappedValue)))
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                              .opacity(self.message.isEmpty ? 0.5 : 1.0)
                              .animation(.linear(duration: 0.5), value: self.message.isEmpty)
                        }
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .clipShape(Circle())
                        .disabled(self.message.isEmpty)
                     }
                     .background(.clear)
                     .padding(EdgeInsets(
                        top: 0.0,
                        leading: 8.0,
                        bottom: 8.0,
                        trailing: 8.0
                     ))
                  }
                  .background {
                     ConcentricRectangle(corners: .concentric(minimum: 24.0), isUniform: true)
                        .fill(.clear)
                        .contentShape(.interaction, ConcentricRectangle(corners: .concentric(minimum: 24.0), isUniform: true))
                  }
                  .padding(0.0)
                  .foregroundStyle(.primary)
                  .glassEffect(.regular, in: ConcentricRectangle(corners: .concentric(minimum: 24.0), isUniform: true))
                  .compositingGroup()
                  .shadow(color: Color(hue: 0.0, saturation: 0.0, brightness: 0.0, opacity: 0.25), radius: 8.0, x: 0.0, y: 0.0)
                  .geometryGroup()
                  .animation(.linear(duration: 0.5),value: self.message)
                  .onTapGesture {
                     self.composerFocused = false
                  }
               }
               .frame(
                  maxWidth: .infinity
               )
               .padding(EdgeInsets(
                  top: 0.0,
                  leading: 16.0,
                  bottom: 8.0,
                  trailing: 16.0
               ))
               .transition(.opacity)
            }
         }
         .frame(
            minWidth: 0.0,
            maxWidth: .infinity,
            minHeight: 0.0,
            maxHeight: .infinity,
            alignment: .topLeading
         )
         .background(Color(uiColor: .systemBackground))
         .onChange(of: self.scenePhase) {
            if self.scenePhase == .active {
               let now = Int(Date().timeIntervalSince1970)
               let elapsed = now - self.timestamp
               
               self.isActive = true
               self.timestamp = now
               
               withAnimation {
                  self.energy = min(self.energy + Double(max(elapsed, 0)) / self.recoveryDuration * self.maxEnergy, self.maxEnergy)
               }
            } else {
               self.isActive = false
               
               if self.scenePhase == .background {
                  if self.isRecording {
                     self.stopRecognize()
                  }
                  
                  withAnimation {
                     self.isPeeking = false
                  }
               }
            }
         }
         .onChange(of: self.shortcut.type) {
            if let type = self.shortcut.type, !type.isEmpty {
               if type[0].isEmpty {
                  self.choices.removeAll()
                  self.showActivity = false
                  self.showDictionary = false
                  self.showGallery = false
                  self.showSettings = false
                  self.shortcut.type = nil
                  
                  Task {
                     await self.talk(word: Word(name: type[1], attributes: [String](Script.shared.words.reduce(into: Set<String>(), { x, y in
                        if y.name == type[1], let attributes = y.attributes {
                           for attribute in attributes {
                              if !x.contains(attribute) {
                                 x.insert(attribute)
                              }
                           }
                        }
                     }))), temperature: self.temperature, multiple: geometry.size.width > geometry.size.height, fallback: false, mute: self.mute)
                  }
               } else if type[0] == "Dictionary" {
                  if self.isRecording {
                     self.stopRecognize()
                  }
                  
                  self.showActivity = false
                  self.showGallery = false
                  self.showSettings = false
                  self.showDictionary = true
                  
                  withAnimation {
                     self.isPeeking = false
                     self.revealMenu = false
                  }
               }
            }
         }
         .onAppear {
            if let type = self.shortcut.type, !type.isEmpty {
               if type[0].isEmpty {
                  self.shortcut.type = nil
                  
                  Task {
                     await self.talk(word: Word(name: type[1], attributes: [String](Script.shared.words.reduce(into: Set<String>(), { x, y in
                        if y.name == type[1], let attributes = y.attributes {
                           for attribute in attributes {
                              if !x.contains(attribute) {
                                 x.insert(attribute)
                              }
                           }
                        }
                     }))), temperature: self.temperature, multiple: geometry.size.width > geometry.size.height, fallback: false, mute: self.mute)
                  }
               } else if type[0] == "Dictionary" {
                  self.showDictionary = true
               }
            }
         }
      }
   }
   
   init() {
      let color = UIColor(named: "AccentColor")!
      let red: CGFloat
      let green: CGFloat
      let blue: CGFloat
      let padding = 8.0
      var image = UIImage(named: "Star")!
      let maximum = max(image.size.width, image.size.height)
      let length = (maximum + padding * 2.0) * image.scale
      let scale = 0.75
      let size = CGSize(width: ceil(length * scale), height: ceil(length * scale))
      
      if let components = color.cgColor.components {
         let index = color.cgColor.numberOfComponents - 2
         
         red = components[min(0, index)]
         green = components[min(1, index)]
         blue = components[min(2, index)]
      } else {
         red = 0.0
         green = 0.0
         blue = 0.0
      }
      
      let rendererFormat = UIGraphicsImageRendererFormat()
      
      rendererFormat.opaque = false
      rendererFormat.scale = 1.0
      rendererFormat.preferredRange = .standard
      
      let renderer = UIGraphicsImageRenderer(size: size, format: rendererFormat)
      
      _ = renderer.image { rendererContext in
         let context = rendererContext.cgContext
         
         if let starImage = image.cgImage {
            context.interpolationQuality = .high
            context.setAllowsAntialiasing(true)
            context.clear(CGRect(x: 0.0, y: 0.0, width: size.width, height: size.height))
            context.translateBy(x: 0.0, y: size.height)
            context.scaleBy(x: scale, y: -scale)
            context.setFillColor(CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [1.0, 1.0, 1.0, 1.0])!)
            context.fill(CGRect(origin: CGPoint.zero, size: CGSize(width: length, height: length)))
            context.setFillColor(CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [0.0, 0.0, 0.0, 1.0])!)
            context.addArc(center: CGPoint(x: (padding + maximum / 2.0) * image.scale, y: (padding + maximum / 2.0) * image.scale), radius: (maximum + padding * 2.0) / 2.0 * image.scale, startAngle: 0.0, endAngle: Double.pi * 2.0, clockwise: true)
            context.fillPath()
            context.clip(to: CGRect(origin: CGPoint(x: (length - CGFloat(starImage.width)) / 2.0, y: (length - CGFloat(starImage.height)) / 2.0), size: CGSize(width: starImage.width, height: starImage.height)), mask: starImage)
            context.setFillColor(CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [1.0, 1.0, 1.0, 1.0])!)
            context.fill(CGRect(origin: CGPoint(x: padding * image.scale, y: padding * image.scale), size: CGSize(width: starImage.width, height: starImage.height)))
            
            if let cgImage = context.makeImage(), let dataProvider = cgImage.dataProvider, let maskImage = CGImage(maskWidth: cgImage.width, height: cgImage.height, bitsPerComponent: cgImage.bitsPerComponent, bitsPerPixel: cgImage.bitsPerPixel, bytesPerRow: cgImage.bytesPerRow, provider: dataProvider, decode: nil, shouldInterpolate: false), let maskedImage = cgImage.masking(maskImage) {
               image = UIImage(cgImage: maskedImage, scale: image.scale, orientation: image.imageOrientation)
            }
         }
      }
      
      self.starImage = image
      self._script = StateObject(wrappedValue: Script.shared)
      self.path = AppStorage(wrappedValue: String(), "path")
      self.accent = AppStorage(wrappedValue: String.init(format: "#%02lx%02lx%02lx", lroundf(Float(red * 255)), lroundf(Float(green * 255)), lroundf(Float(blue * 255))), "accent")
      self._selection = State(initialValue: self.path.wrappedValue)
   }
   
   private func makeMenu(geometryProxy: GeometryProxy) -> some View {
      return ZStack {
         Rectangle()
            .fill(.clear)
            .frame(
               width: self.horizontalSizeClass == .compact && self.verticalSizeClass == .regular && geometryProxy.size.width < geometryProxy.size.height ? geometryProxy.size.width - 32.0 : geometryProxy.size.width / 2.0 - 32.0,
               height: (geometryProxy.size.height + geometryProxy.safeAreaInsets.top + geometryProxy.safeAreaInsets.bottom) / 2.0 - geometryProxy.safeAreaInsets.bottom - 72.0
            )
            .glassEffect(.regular, in: ConcentricRectangle(corners: .concentric(minimum: 24.0), isUniform: true))
            .compositingGroup()
            .shadow(color: Color(hue: 0.0, saturation: 0.0, brightness: 0.0, opacity: 0.25), radius: 8.0, x: 0.0, y: 0.0)
         ScrollView([.vertical]) {
            LazyVStack(spacing: 0.0) {
               VStack(spacing: 0.0) {
                  Button(action: {
                     if self.prompt.2 && self.prompt.5 == 0 {
                        self.prompt = (self.prompt.0, self.prompt.1, false, self.prompt.3, self.prompt.4, self.prompt.5, CACurrentMediaTime())
                     } else if self.prompt.5 > 0 {
                        if let url = self.prompt.4[self.prompt.5 - 1].1 {
                           openURL(url)
                           
                           withAnimation {
                              self.revealMenu = false
                           }
                        } else if self.energy >= 1.0 {
                           self.choices.removeAll()
                           self.timestamp = Int(Date().timeIntervalSince1970)
                           
                           withAnimation {
                              self.energy -= 1.0
                              self.revealMenu = false
                           }
                           
                           Task {
                              await self.talk(word: Word(name: self.prompt.4[self.prompt.5 - 1].0), temperature: self.temperature, multiple: geometryProxy.size.width > geometryProxy.size.height, fallback: true, mute: self.mute)
                           }
                        } else {
                           self.shakes += 1
                        }
                     } else if let word = self.prompt.1 {
                        if self.energy >= 1.0 {
                           self.choices.removeAll()
                           self.timestamp = Int(Date().timeIntervalSince1970)
                           
                           withAnimation {
                              self.energy -= 1.0
                              self.revealMenu = false
                           }
                           
                           Task {
                              await self.talk(word: word, temperature: self.temperature, multiple: geometryProxy.size.width > geometryProxy.size.height, fallback: true, mute: self.mute)
                           }
                        } else {
                           self.shakes += 1
                        }
                     }
                  }) {
                     ZStack(alignment: .center) {
                        Prompt(active: self.isActive, input: self.prompt, accent: self.convert(from: self.accent.wrappedValue), font: UIFont.systemFont(ofSize: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0), weight: .semibold))
                           .frame(
                              height: ceil(UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).lineHeight),
                              alignment: .center
                           )
                           .offset(y: ceil(UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).leading / 2.0))
                           .padding(0.0)
                           .background(.clear)
                           
                        if self.prompt.0 == nil {
                           Image(systemName: "exclamationmark.triangle")
                              .symbolRenderingMode(.monochrome)
                              .frame(
                                 width: 16.0,
                                 height: 16.0,
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(.primary)
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                              .zIndex(1)
                              .transition(.opacity.animation(.linear(duration: 0.5)))
                        }
                     }
                  }
                  .frame(
                     height: ceil(UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).lineHeight - UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).leading),
                     alignment: .center
                  )
                  .padding(EdgeInsets(
                     top: 8.0,
                     leading: 16.0,
                     bottom: 4.0,
                     trailing: 16.0
                  ))
                  .background(.clear)
                  VStack(spacing: 8.0) {
                     Text(String(format: "%.0f%%", floor(self.energy / self.maxEnergy * 100.0)))
                        .foregroundStyle(.primary)
                        .font(.caption)
                        .fontWeight(.semibold)
                        .lineLimit(1)
                        .truncationMode(.tail)
                        .padding(EdgeInsets(
                           top: 0.0,
                           leading: 0.0,
                           bottom: 0.0,
                           trailing: 0.0
                        ))
                        .contentTransition(.numericText(value: self.energy / self.maxEnergy * 100.0))
                     GeometryReader { geometry in
                        ZStack(alignment: .leading) {
                           Rectangle()
                              .fill(Color(self.convert(from: self.accent.wrappedValue)).opacity(0.25))
                           Rectangle()
                              .fill(Color(self.convert(from: self.accent.wrappedValue)))
                              .frame(width: geometry.size.width * CGFloat(self.energy / self.maxEnergy))
                        }
                        .clipShape(Capsule())
                     }
                     .frame(
                        width: 64.0,
                        height: 4.0
                     )
                     .keyframeAnimator(initialValue: 0, trigger: self.shakes, content: { view, value in
                        view
                           .offset(x: value)
                     }, keyframes: { _ in
                        MoveKeyframe(5.0)
                        LinearKeyframe(5.0, duration: 0.5 / 15.0)
                        MoveKeyframe(0.0)
                        LinearKeyframe(0.0, duration: 0.5 / 15.0)
                        MoveKeyframe(-5.0)
                        LinearKeyframe(-5.0, duration: 0.5 / 15.0)
                        MoveKeyframe(4.0)
                        LinearKeyframe(4.0, duration: 0.5 / 15.0)
                        MoveKeyframe(0.0)
                        LinearKeyframe(0.0, duration: 0.5 / 15.0)
                        MoveKeyframe(-4.0)
                        LinearKeyframe(-4.0, duration: 0.5 / 15.0)
                        MoveKeyframe(3.0)
                        LinearKeyframe(3.0, duration: 0.5 / 15.0)
                        MoveKeyframe(0.0)
                        LinearKeyframe(0.0, duration: 0.5 / 15.0)
                        MoveKeyframe(-3.0)
                        LinearKeyframe(-3.0, duration: 0.5 / 15.0)
                        MoveKeyframe(2.0)
                        LinearKeyframe(2.0, duration: 0.5 / 15.0)
                        MoveKeyframe(0.0)
                        LinearKeyframe(0.0, duration: 0.5 / 15.0)
                        MoveKeyframe(-2.0)
                        LinearKeyframe(-2.0, duration: 0.5 / 15.0)
                        MoveKeyframe(1.0)
                        LinearKeyframe(1.0, duration: 0.5 / 15.0)
                        MoveKeyframe(0.0)
                        LinearKeyframe(0.0, duration: 0.5 / 15.0)
                        MoveKeyframe(-1.0)
                        LinearKeyframe(-1.0, duration: 0.5 / 15.0)
                        MoveKeyframe(0.0)
                     })
                  }
                  .frame(maxWidth: .infinity)
                  .padding(
                     EdgeInsets(
                        top: 0.0,
                        leading: 16.0,
                        bottom: 0.0,
                        trailing: 16.0
                     )
                  )
                  .animation(.easeInOut(duration: 0.5), value: self.energy)
                  HStack(alignment: .center, spacing: 0.0) {
                     VStack(alignment: .center, spacing: 0.0) {
                        if !self.prompt.4.isEmpty {
                           Button(action: {
                              let index = (self.prompt.5 - 1) % (self.prompt.4.count + 1)
                              
                              if index > 0 {
                                 self.prompt = (self.prompt.4[index - 1].0, self.prompt.1, self.prompt.2, self.prompt.3, self.prompt.4, index, CACurrentMediaTime())
                              } else if index == 0 {
                                 if let word = self.prompt.1 {
                                    self.prompt = (word.name, self.prompt.1, self.prompt.2, self.prompt.3, self.prompt.4, index, CACurrentMediaTime())
                                 } else {
                                    self.prompt = (nil, self.prompt.1, self.prompt.2, self.prompt.3, self.prompt.4, index, CACurrentMediaTime())
                                 }
                              } else {
                                 self.prompt = (self.prompt.4[self.prompt.4.count - 1].0, self.prompt.1, self.prompt.2, self.prompt.3, self.prompt.4, self.prompt.4.count, CACurrentMediaTime())
                              }
                           }) {
                              Image(systemName: "chevron.backward")
                                 .frame(
                                    width: 16.0,
                                    height: 16.0,
                                    alignment: .center
                                 )
                                 .background(.clear)
                                 .foregroundStyle(.primary)
                                 .font(
                                    .system(size: 16.0)
                                 )
                                 .bold()
                           }
                           .frame(
                              alignment: .center
                           )
                           .padding(16.0)
                           .background(.clear)
                           .transition(.opacity.animation(.linear(duration: 0.5)))
                        }
                     }
                     .frame(
                        minWidth: 0.0,
                        maxWidth: .infinity
                     )
                     VStack(alignment: .center, spacing: 0.0) {
                        Button(action: {
                           if let word = self.prompt.1, (word.attributes == nil || !word.attributes!.isEmpty) && self.prompt.2 {
                              self.prompt = (word.name, self.prompt.1, false, self.prompt.3, self.prompt.4, 0, CACurrentMediaTime())
                           } else if !self.script.words.isEmpty {
                              let samples = 10
                              var letterSet = Set<Character>()
                              var modifiers = [String]()
                              var words = [Word]()
                              
                              for _ in 0..<samples {
                                 let word = self.script.words[Int.random(in: 0..<self.script.words.count)]
                                 
                                 for i in 0..<word.name.count {
                                    let character = word.name[word.name.index(word.name.startIndex, offsetBy: i)]
                                    
                                    if !letterSet.contains(character) && !character.isNewline && !character.isWhitespace {
                                       letterSet.insert(character)
                                    }
                                 }
                                 
                                 if let attributes = word.attributes, attributes.isEmpty {
                                    modifiers.append(word.name)
                                 } else {
                                    words.append(word)
                                 }
                              }
                              
                              if words.isEmpty {
                                 self.prompt = (nil, nil, false, nil, self.prompt.4, 0, CACurrentMediaTime())
                              } else {
                                 var word = words[Int.random(in: 0..<words.count)]
                                 
                                 if Double.random(in: 0.0..<1.0) < Double(modifiers.count) / Double(samples) {
                                    let modifier = modifiers[Int.random(in: 0..<modifiers.count)]
                                    
                                    if modifier.allSatisfy({ $0.isASCII }) && word.name.allSatisfy({ $0.isASCII }) {
                                       word.name = modifier + String("\u{0020}\u{000A}") + word.name
                                    } else {
                                       word.name = modifier + "\n" + word.name
                                    }
                                 }
                                 
                                 self.prompt = (word.name, word, false, letterSet, self.prompt.4, 0, CACurrentMediaTime())
                              }
                           }
                        }) {
                           VStack(alignment: .center, spacing: 8.0) {
                              Image(systemName: "dice")
                                 .symbolRenderingMode(.monochrome)
                                 .frame(
                                    width: 16.0,
                                    height: 16.0,
                                    alignment: .center
                                 )
                                 .background(.clear)
                                 .foregroundStyle(.primary)
                                 .font(
                                    .system(size: 16.0)
                                 )
                                 .bold()
                              Text("Randomize")
                                 .foregroundStyle(.primary)
                                 .font(.caption)
                                 .fontWeight(.semibold)
                                 .lineLimit(1)
                                 .truncationMode(.tail)
                                 .textCase(.uppercase)
                           }
                        }
                        .frame(
                           alignment: .center
                        )
                        .padding(16.0)
                        .background(.clear)
                        .disabled(self.script.words.isEmpty)
                        .opacity(self.script.words.isEmpty ? 0.5 : 1.0)
                     }
                     .frame(
                        minWidth: 0.0,
                        maxWidth: .infinity
                     )
                     VStack(alignment: .center, spacing: 0.0) {
                        if !self.prompt.4.isEmpty {
                           Button(action: {
                              let index = (self.prompt.5 + 1) % (self.prompt.4.count + 1)
                              
                              if index > 0 {
                                 self.prompt = (self.prompt.4[index - 1].0, self.prompt.1, self.prompt.2, self.prompt.3, self.prompt.4, index, CACurrentMediaTime())
                              } else if let word = self.prompt.1 {
                                 self.prompt = (word.name, self.prompt.1, self.prompt.2, self.prompt.3, self.prompt.4, index, CACurrentMediaTime())
                              } else {
                                 self.prompt = (nil, self.prompt.1, self.prompt.2, self.prompt.3, self.prompt.4, index, CACurrentMediaTime())
                              }
                           }) {
                              Image(systemName: "chevron.forward")
                                 .frame(
                                    width: 16.0,
                                    height: 16.0,
                                    alignment: .center
                                 )
                                 .background(.clear)
                                 .foregroundStyle(.primary)
                                 .font(
                                    .system(size: 16.0)
                                 )
                                 .bold()
                           }
                           .frame(
                              alignment: .center
                           )
                           .padding(16.0)
                           .background(.clear)
                           .transition(.opacity.animation(.linear(duration: 0.5)))
                        }
                     }
                     .frame(
                        minWidth: 0.0,
                        maxWidth: .infinity
                     )
                  }
               }
               .padding(0.0)
               .background(.clear)
               Rectangle()
                  .fill(.secondary.opacity(0.1))
                  .frame(
                     height: 1.0
                  )
               HStack(alignment: .center, spacing: 0.0) {
                  VStack(alignment: .center, spacing: 0.0) {
                     Button(action: {
                        self.showActivity = true
                        
                        withAnimation {
                           self.isPeeking = false
                           self.revealMenu = false
                        }
                     }) {
                        VStack(alignment: .center, spacing: 8.0) {
                           Image(systemName: "chart.bar")
                              .frame(
                                 width: 16.0,
                                 height: 16.0,
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(.primary)
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                           Text("Activity")
                              .foregroundStyle(.primary)
                              .font(.caption)
                              .fontWeight(.semibold)
                              .lineLimit(1)
                              .truncationMode(.tail)
                              .textCase(.uppercase)
                        }
                     }
                     .frame(
                        alignment: .center
                     )
                     .padding(16.0)
                     .background(.clear)
                  }
                  .frame(
                     minWidth: 0.0,
                     maxWidth: .infinity
                  )
                  Rectangle()
                     .fill(.secondary.opacity(0.1))
                     .frame(
                        width: 1.0
                     )
                  VStack(alignment: .center, spacing: 0.0) {
                     Button(action: {
                        self.showDictionary = true
                        
                        withAnimation {
                           self.isPeeking = false
                           self.revealMenu = false
                        }
                     }) {
                        VStack(alignment: .center, spacing: 8.0) {
                           Image(systemName: "book")
                              .frame(
                                 width: 16.0,
                                 height: 16.0,
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(.primary)
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                           Text("Dictionary")
                              .foregroundStyle(.primary)
                              .font(.caption)
                              .fontWeight(.semibold)
                              .lineLimit(1)
                              .truncationMode(.tail)
                              .textCase(.uppercase)
                        }
                     }
                     .frame(
                        alignment: .center
                     )
                     .padding(16.0)
                     .background(.clear)
                  }
                  .frame(
                     minWidth: 0.0,
                     maxWidth: .infinity
                  )
                  Rectangle()
                     .fill(.secondary.opacity(0.1))
                     .frame(
                        width: 1.0
                     )
                  VStack(alignment: .center, spacing: 0.0) {
                     Button(action: {
                        self.showGallery = true
                        
                        withAnimation {
                           self.isPeeking = false
                           self.revealMenu = false
                        }
                     }) {
                        VStack(alignment: .center, spacing: 8.0) {
                           Image(systemName: "photo")
                              .frame(
                                 width: 16.0,
                                 height: 16.0,
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(.primary)
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                           Text("Gallery")
                              .foregroundStyle(.primary)
                              .font(.caption)
                              .fontWeight(.semibold)
                              .lineLimit(1)
                              .truncationMode(.tail)
                              .textCase(.uppercase)
                        }
                     }
                     .frame(
                        alignment: .center
                     )
                     .padding(16.0)
                     .background(.clear)
                  }
                  .frame(
                     minWidth: 0.0,
                     maxWidth: .infinity
                  )
               }
               Rectangle()
                  .fill(.secondary.opacity(0.1))
                  .frame(
                     height: 1.0
                  )
               HStack(alignment: .center, spacing: 0.0) {
                  VStack(alignment: .center, spacing: 0.0) {
                     Button(action: {
                        self.showSettings = true
                        
                        withAnimation {
                           self.isPeeking = false
                           self.revealMenu = false
                        }
                     }) {
                        VStack(alignment: .center, spacing: 8.0) {
                           Image(systemName: "gearshape")
                              .frame(
                                 width: 16.0,
                                 height: 16.0,
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(.primary)
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                           Text("Settings")
                              .foregroundStyle(.primary)
                              .font(.caption)
                              .fontWeight(.semibold)
                              .lineLimit(1)
                              .truncationMode(.tail)
                              .textCase(.uppercase)
                        }
                     }
                     .frame(
                        alignment: .center
                     )
                     .padding(16.0)
                     .background(.clear)
                  }
                  .frame(
                     minWidth: 0.0,
                     maxWidth: .infinity
                  )
               }
               Rectangle()
                  .fill(.secondary.opacity(0.1))
                  .frame(
                     height: 1.0
                  )
               ForEach(self.labels.indices, id: \.self) { index in
                  let type = self.labels[index]
                  let checked = self.types & Int(pow(2.0, Double(index)))
                  
                  VStack(spacing: 0.0) {
                     if index > 0 {
                        Rectangle()
                           .fill(.secondary.opacity(0.1))
                           .frame(
                              height: 1.0
                           )
                     }
                     
                     Button(action: {
                        if checked > 0 {
                           var x = 0
                           
                           for i in 0..<self.labels.count {
                              if index != i && self.types & Int(pow(2.0, Double(i))) > 0 {
                                 x = x | Int(pow(2.0, Double(i)))
                              }
                           }
                           
                           withAnimation(.easeInOut(duration: 0.5)) {
                              self.types = self.types & x
                           }
                        } else {
                           withAnimation(.easeInOut(duration: 0.5)) {
                              self.types = self.types | Int(pow(2.0, Double(index)))
                           }
                        }
                     }) {
                        if checked > 0 {
                           HStack(alignment: .center, spacing: 16.0) {
                              Image(systemName: "checkmark")
                                 .frame(
                                    width: 16.0,
                                    height: 16.0,
                                    alignment: .center
                                 )
                                 .background(.clear)
                                 .foregroundStyle(Color(self.convert(from: self.accent.wrappedValue)))
                                 .font(
                                    .system(size: 16.0)
                                 )
                                 .bold()
                              Text(type)
                                 .foregroundStyle(.primary)
                                 .font(.subheadline)
                                 .fontWeight(.semibold)
                                 .lineLimit(1)
                                 .truncationMode(.tail)
                                 .textCase(.uppercase)
                           }
                           .transition(.opacity.animation(.linear(duration: 0.5)))
                        } else {
                           Text(type)
                              .foregroundStyle(.primary)
                              .font(.subheadline)
                              .fontWeight(.semibold)
                              .lineLimit(1)
                              .truncationMode(.tail)
                              .textCase(.uppercase)
                              .transition(.opacity.animation(.linear(duration: 0.5)))
                        }
                     }
                     .frame(
                        alignment: .center
                     )
                     .padding(16.0)
                     .background(.clear)
                  }
                  .transition(.opacity)
               }
            }
            .frame(
               width: self.horizontalSizeClass == .compact && self.verticalSizeClass == .regular && geometryProxy.size.width < geometryProxy.size.height ? geometryProxy.size.width - 32.0 : geometryProxy.size.width / 2.0 - 32.0,
            )
            .padding(0.0)
            .background(.clear)
            .foregroundStyle(.primary, .secondary)
         }
         .frame(
            height: (geometryProxy.size.height + geometryProxy.safeAreaInsets.top + geometryProxy.safeAreaInsets.bottom) / 2.0 - geometryProxy.safeAreaInsets.bottom - 72.0
         )
         .padding(0.0)
         .background(.clear)
         .clipShape(ConcentricRectangle(corners: .concentric(minimum: 24.0), isUniform: true))
      }
      .padding(EdgeInsets(
         top: 0.0,
         leading: 0.0,
         bottom: geometryProxy.safeAreaInsets.bottom + 72.0,
         trailing: 0.0
      ))
      .background(.clear)
      .transition(AnyTransition.opacity.combined(with: .scale(scale: 0.5, anchor: .bottom)))
      .onDisappear {
         self.shakes = 0
      }
   }
   
   private func talk(word: Word, temperature: Double, multiple: Bool, fallback: Bool, mute: Bool) async {
      var queue = Script.shared.characters
      
      if let first = queue.first {
         let input = word.name.filter { !$0.isNewline }
         var logs = [(id: UUID?, from: String?, to: String?, group: Double, raw: String?, content: (text: String?, image: CGImage?), choices: [String]?)]()
         let attributes = word.attributes ?? []
         let generateRequired: Bool
         let time: Double
         var sequences = [(String, UUID?, String, Sequence, Double?, [(String, URL?)]?)]()
         
         if multiple {
            queue.removeFirst()
            
            if let last = self.logs.last {
               var isContinuous = false
               
               for log in self.logs {
                  if log.group == last.group {
                     if log.from == nil, let choices = log.choices, choices.contains(where: { $0 == input }) {
                        isContinuous = true
                     }
                     
                     logs.append(log)
                  }
               }
               
               if isContinuous {
                  generateRequired = true
                  time = last.group
               } else {
                  logs.removeAll()
                  generateRequired = attributes.isEmpty || !first.sequences.contains(where: { $0.name == "Activate" })
                  time = CACurrentMediaTime()
               }
            } else {
               generateRequired = attributes.isEmpty || !first.sequences.contains(where: { $0.name == "Activate" })
               time = CACurrentMediaTime()
            }
         } else {
            queue.removeAll()
            
            if let last = self.logs.last, self.logs.contains(where: { x in
               if x.from == nil && x.group == last.group, let choices = x.choices {
                  return choices.contains(where: { $0 == input })
               }
               
               return false
            }) {
               generateRequired = true
               time = last.group
            } else {
               generateRequired = attributes.isEmpty || !first.sequences.contains(where: { $0.name == "Activate" })
               time = CACurrentMediaTime()
            }
         }
         
         if var prompt = first.prompt, generateRequired {
            withAnimation(.easeOut(duration: 0.5)) {
               self.isLoading = true
            }
            
            if let memory = (await Task.detached {
               if let data = self.load() {
                  return String(data: data, encoding: .utf8)
               }
               
               return nil
            }.value) {
               prompt.append("\n\(memory)")
            }
            
            var messages: [[String: Any]] = [["role": "system", "content": prompt]]
            var i = logs.count - 1
            
            while i > 0 {
               if let from = logs[i].from, from != first.name && logs[i - 1].from == first.name {
                  var parts = [[String: Any]]()
                  
                  if let text = logs[i].content.text {
                     parts.append(["type": "text", "text": text])
                  }
                  
                  if let image = logs[i].content.image {
                     if let dataURL = (await Task.detached {
                        var dataURL: String? = nil
                        
                        if let resizedImage = self.resize(image: image) {
                           dataURL = self.convert(image: resizedImage)
                        }
                        
                        return dataURL
                     }.value) {
                        parts.append(["type": "image", "image": dataURL])
                     }
                  }
                  
                  if !parts.isEmpty {
                     if let raw = logs[i - 1].raw {
                        messages.insert(["role": "user", "content": parts], at: 1)
                        messages.insert(["role": "assistant", "content": raw], at: 1)
                     } else if let text = logs[i - 1].content.text {
                        messages.insert(["role": "user", "content": parts], at: 1)
                        messages.insert(["role": "assistant", "content": text], at: 1)
                     }
                  }
                  
                  i -= 2
               } else {
                  i -= 1
               }
            }
            
            if messages.count == 1 {
               var i = self.logs.count - 1
               
               while i >= 0 {
                  if self.logs[i].from == first.name && self.logs[i].to == nil {
                     if i - 1 >= 0 && self.logs[i].group == self.logs[i - 1].group && self.logs[i - 1].from == nil && self.logs[i - 1].to == first.name {
                        var parts = [[String: Any]]()
                        
                        if let text = self.logs[i - 1].content.text {
                           parts.append(["type": "text", "text": text])
                        }
                        
                        if let image = self.logs[i - 1].content.image {
                           if let dataURL = (await Task.detached {
                              var dataURL: String? = nil
                              
                              if let resizedImage = self.resize(image: image) {
                                 dataURL = self.convert(image: resizedImage)
                              }
                              
                              return dataURL
                           }.value) {
                              parts.append(["type": "image", "image": dataURL])
                           }
                        }
                        
                        if !parts.isEmpty {
                           if let raw = self.logs[i].raw {
                              messages.insert(["role": "assistant", "content": raw], at: 1)
                              messages.insert(["role": "user", "content": parts], at: 1)
                           } else if let text = self.logs[i].content.text {
                              messages.insert(["role": "assistant", "content": text], at: 1)
                              messages.insert(["role": "user", "content": parts], at: 1)
                           }
                        }
                        
                        i -= 1
                     } else if let raw = self.logs[i].raw {
                        messages.insert(["role": "assistant", "content": raw], at: 1)
                     } else if let text = self.logs[i].content.text {
                        messages.insert(["role": "assistant", "content": text], at: 1)
                     }
                  }
                  
                  i -= 1
               }
               
               messages.append(["role": "user", "content": [["type": "text", "text": input]]])
            }
            
            if let (output, content, likability, terms, state, choices, memory, voice) = await self.generate(messages: messages, voice: mute ? nil : await self.sample(path: first.path, sequences: first.sequences), language: first.language, temperature: temperature) {
               var sequence = Sequence(name: "Activate", state: nil)
               let id = UUID()
               var candidates = terms.reduce(into: [(target: String, words: [Word])]()) { output, value in
                  let term = value.filter { !$0.isEmpty }

                  guard let word = term.last else {
                     return
                  }

                  if term.count == 1 {
                     output.append((target: word, words: [Word(name: word, attributes: nil)]))
                  } else {
                     for index in 0..<term.count - 1 {
                        let parts = Array(term[index...])
                        let separator = parts.allSatisfy { $0.allSatisfy { $0.isASCII } } ? String("\u{0020}") : String()
                        
                        output.append((target: parts.joined(separator: separator), words: parts.enumerated().map { Word(name: $0.element, attributes: $0.offset < parts.count - 1 ? [] : nil) }))
                     }
                  }
               }
               
               candidates.sort { $0.target.count > $1.target.count }
               
               var inlines: [(text: String, attributes: [String]?)] = content.isEmpty ? [] : [(content, nil)]
               var words = [Word]()
               
               for candidate in candidates {
                  inlines = inlines.reduce(into: []) { output, inline in
                     guard inline.attributes == nil else {
                        output.append(inline)
                        
                        return
                     }

                     var text = inline.text

                     while let range = text.range(of: candidate.target, options: .caseInsensitive) {
                        if range.lowerBound != text.startIndex {
                           output.append((text: String(text[..<range.lowerBound]), attributes: nil))
                        }
                        
                        var inline = String(text[range])

                        if candidate.words.count > 1, let lastWord = candidate.words.last?.name, let lastRange = inline.range(of: lastWord, options: [.caseInsensitive, .backwards]) {
                           inline.insert("\n", at: lastRange.lowerBound)
                        }
                        
                        output.append((text: inline, attributes: []))
                        
                        for word in candidate.words {
                           if !words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) && !Script.shared.words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) {
                              words.append(word)
                           }
                        }

                        text = String(text[range.upperBound...])
                     }

                     if !text.isEmpty {
                        output.append((text: text, attributes: nil))
                     }
                  }
               }
               
               sequence.append(.message(Message(id: id, inlines: inlines)))
               
               if let voice {
                  sequence.append(.audio(voice))
               }
               
               sequence.append(.sequence(Sequence(name: "Emote", state: state ?? String())))
               sequences.append((first.name, id, output, sequence, likability, choices))
               
               if !words.isEmpty {
                  self.discoveries.append(contentsOf: words)
               }
               
               if let memory {
                  await Task.detached {
                     if let data = memory.data(using: .utf8) {
                        self.save(data)
                     }
                  }.value
               }
               
               while !queue.isEmpty {
                  let character = queue.removeFirst()
                  
                  if var prompt = character.prompt {
                     if let memory = (await Task.detached {
                        if let data = self.load() {
                           return String(data: data, encoding: .utf8)
                        }
                        
                        return nil
                     }.value) {
                        prompt.append("\n\(memory)")
                     }
                     
                     var messages: [[String: Any]] = [["role": "system", "content": prompt], ["role": "user", "content": [["type": "text", "text": content]]]]
                     var i = logs.count - 1
                     
                     while i > 0 {
                        if logs[i].from == character.name {
                           var parts = [[String: Any]]()
                           
                           if let text = logs[i - 1].content.text {
                              parts.append(["type": "text", "text": text])
                           }
                           
                           if let image = logs[i - 1].content.image {
                              if let dataURL = (await Task.detached {
                                 var dataURL: String? = nil
                                 
                                 if let resizedImage = self.resize(image: image) {
                                    dataURL = self.convert(image: resizedImage)
                                 }
                                 
                                 return dataURL
                              }.value) {
                                 parts.append(["type": "image", "image": dataURL])
                              }
                           }
                           
                           if !parts.isEmpty {
                              if let raw = logs[i].raw {
                                 messages.insert(["role": "assistant", "content": raw], at: 1)
                                 messages.insert(["role": "user", "content": parts], at: 1)
                              } else if let text = logs[i].content.text {
                                 messages.insert(["role": "assistant", "content": text], at: 1)
                                 messages.insert(["role": "user", "content": parts], at: 1)
                              }
                           }
                           
                           i -= 2
                        } else {
                           i -= 1
                        }
                     }
                     
                     if let (output, content, _, terms, state, _, memory, voice) = await self.generate(messages: messages, voice: mute ? nil : await self.sample(path: character.path, sequences: character.sequences), language: character.language, temperature: temperature) {
                        var sequence = Sequence(name: "Activate", state: nil)
                        let id = UUID()
                        var candidates = terms.reduce(into: [(target: String, words: [Word])]()) { output, value in
                           let term = value.filter { !$0.isEmpty }

                           guard let word = term.last else {
                              return
                           }

                           if term.count == 1 {
                              output.append((target: word, words: [Word(name: word, attributes: nil)]))
                           } else {
                              for index in 0..<term.count - 1 {
                                 let parts = Array(term[index...])
                                 let separator = parts.allSatisfy { $0.allSatisfy { $0.isASCII } } ? String("\u{0020}") : String()
                                 
                                 output.append((target: parts.joined(separator: separator), words: parts.enumerated().map { Word(name: $0.element, attributes: $0.offset < parts.count - 1 ? [] : nil) }))
                              }
                           }
                        }
                        
                        candidates.sort { $0.target.count > $1.target.count }
                        
                        var inlines: [(text: String, attributes: [String]?)] = content.isEmpty ? [] : [(content, nil)]
                        var words = [Word]()
                        
                        for candidate in candidates {
                           inlines = inlines.reduce(into: []) { output, inline in
                              guard inline.attributes == nil else {
                                 output.append(inline)
                                 
                                 return
                              }

                              var text = inline.text

                              while let range = text.range(of: candidate.target, options: .caseInsensitive) {
                                 if range.lowerBound != text.startIndex {
                                    output.append((text: String(text[..<range.lowerBound]), attributes: nil))
                                 }
                                 
                                 var inline = String(text[range])

                                 if candidate.words.count > 1, let lastWord = candidate.words.last?.name, let lastRange = inline.range(of: lastWord, options: [.caseInsensitive, .backwards]) {
                                    inline.insert("\n", at: lastRange.lowerBound)
                                 }
                                 
                                 output.append((text: inline, attributes: []))
                                 
                                 for word in candidate.words {
                                    if !words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) && !Script.shared.words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) {
                                       words.append(word)
                                    }
                                 }

                                 text = String(text[range.upperBound...])
                              }

                              if !text.isEmpty {
                                 output.append((text: text, attributes: nil))
                              }
                           }
                        }
                        
                        sequence.append(.message(Message(id: id, inlines: inlines)))
                        
                        if let voice {
                           sequence.append(.audio(voice))
                        }
                        
                        sequence.append(.sequence(Sequence(name: "Emote", state: state ?? String())))
                        sequences.append((character.name, id, output, sequence, nil, nil))
                        
                        if !words.isEmpty {
                           self.discoveries.append(contentsOf: words)
                        }
                        
                        if let memory {
                           await Task.detached {
                              if let data = memory.data(using: .utf8) {
                                 self.save(data)
                              }
                           }.value
                        }
                     } else {
                        break
                     }
                  } else {
                     break
                  }
               }
            }
            
            withAnimation(.easeIn(duration: 0.5)) {
               self.isLoading = false
            }
         }
         
         if sequences.isEmpty {
            var i = 0
            var term = String()
            var modifier = String()
            let tempSequences = Script.shared.characters.reduce(into: [Sequence](), { x, y in
               if y.name == first.name {
                  for sequence in y.sequences {
                     if sequence.name == "Activate" {
                        x.append(sequence)
                     }
                  }
               }
            })
            var oldSequences: [Sequence]? = nil
            
            while i < word.name.count {
               let character = word.name[word.name.index(word.name.startIndex, offsetBy: i)]
               
               if character.isNewline {
                  modifier.append(contentsOf: term)
                  term.removeAll()
               } else {
                  term.append(character)
               }
               
               i += 1
            }
            
            await Script.shared.run(name: first.name, sequences: tempSequences, words: [Word(name: term, attributes: word.attributes)], temperature: temperature) { x in
               if !x.isEmpty {
                  var y = x
                  
                  y.append(Sequence(name: String()))
                  oldSequences = y
               }
               
               return []
            }
            
            if let oldSequences {
               var content = [String]()
               var newSequences = [Sequence]()
               
               if !mute, let prompt = await self.sample(path: first.path, sequences: first.sequences), let language = first.language {
                  var generateRequired = false
                  
                  if modifier.isEmpty {
                     for sequence in oldSequences {
                        var tempSequence = Sequence(name: sequence.name)
                        
                        for (i, step) in sequence.enumerated() {
                           if case .message(let message) = step {
                              let s = message.reduce(into: String()) { content, inline in
                                 if inline.attributes == nil {
                                    content.append(inline.text)
                                 } else {
                                    content.append(inline.text.filter { !$0.isNewline })
                                 }
                              }
                              
                              if i + 1 < sequence.count, case .sound = sequence[i + 1] {
                                 tempSequence.append(.message(message))
                              } else {
                                 tempSequence.append(.synthesis(message, s))
                                 generateRequired = true
                              }
                              
                              content.append(s)
                           } else {
                              tempSequence.append(step)
                           }
                        }
                        
                        newSequences.append(tempSequence)
                     }
                  } else {
                     for sequence in oldSequences {
                        var tempSequence = Sequence(name: sequence.name)
                        
                        for (i, step) in sequence.enumerated() {
                           if case .message(let message) = step {
                              var m = Message()
                              var s = String()
                              
                              for j in 0..<message.count {
                                 var isEqual = true
                                 
                                 if let a = message[j].attributes, message[j].text == term && a.count == attributes.count {
                                    for k in 0..<attributes.count {
                                       if attributes[k] != a[k] {
                                          isEqual = false
                                          
                                          break
                                       }
                                    }
                                 } else {
                                    isEqual = false
                                 }
                                 
                                 if isEqual {
                                    m.append((text: modifier + "\n" + term, attributes: message[j].attributes))
                                    s.append(modifier + term)
                                 } else {
                                    m.append((text: message[j].text, attributes: message[j].attributes))
                                    s.append(message[j].text)
                                 }
                              }
                              
                              if i + 1 < sequence.count, case .sound = sequence[i + 1] {
                                 tempSequence.append(.message(m))
                              } else {
                                 tempSequence.append(.synthesis(m, s))
                                 generateRequired = true
                              }
                              
                              content.append(s)
                           } else {
                              tempSequence.append(step)
                           }
                        }
                        
                        newSequences.append(tempSequence)
                     }
                  }
                  
                  if generateRequired {
                     withAnimation(.easeOut(duration: 0.5)) {
                        self.isLoading = true
                     }
                     
                     for i in 0..<newSequences.count {
                        var tempSequence = Sequence(name: newSequences[i].name)
                        
                        for step in newSequences[i] {
                           if case .synthesis(let message, let input) = step {
                              tempSequence.append(.message(message))
                              
                              if let wave = await self.generate(prompt: prompt, input: input, language: language, temperature: temperature) {
                                 tempSequence.append(.audio(wave))
                              }
                           } else {
                              tempSequence.append(step)
                           }
                        }
                        
                        newSequences[i] = tempSequence
                     }
                     
                     withAnimation(.easeIn(duration: 0.5)) {
                        self.isLoading = false
                     }
                  }
               } else if modifier.isEmpty {
                  for sequence in oldSequences {
                     var tempSequence = Sequence(name: sequence.name)
                     
                     for step in sequence {
                        if case .message(let message) = step {
                           tempSequence.append(.message(message))
                           content.append(message.reduce(into: String(), { content, inline in
                              if inline.attributes == nil {
                                 content.append(inline.text)
                              } else {
                                 content.append(inline.text.filter { !$0.isNewline })
                              }
                           }))
                        } else {
                           tempSequence.append(step)
                        }
                     }
                     
                     newSequences.append(tempSequence)
                  }
               } else {
                  for sequence in oldSequences {
                     var tempSequence = Sequence(name: sequence.name)
                     
                     for step in sequence {
                        if case .message(let message) = step {
                           var m = Message()
                           var s = String()
                           
                           for i in 0..<message.count {
                              var isEqual = true
                              
                              if let a = message[i].attributes, message[i].text == term && a.count == attributes.count {
                                 for j in 0..<attributes.count {
                                    if attributes[j] != a[j] {
                                       isEqual = false
                                       
                                       break
                                    }
                                 }
                              } else {
                                 isEqual = false
                              }
                              
                              if isEqual {
                                 m.append((text: modifier + "\n" + term, attributes: message[i].attributes))
                                 s.append(modifier + term)
                              } else {
                                 m.append((text: message[i].text, attributes: message[i].attributes))
                                 s.append(message[i].text)
                              }
                           }
                           
                           tempSequence.append(.message(m))
                           content.append(s)
                        } else {
                           tempSequence.append(step)
                        }
                     }
                     
                     newSequences.append(tempSequence)
                  }
               }
               
               self.logs.append((id: nil, from: nil, to: first.name, group: time, raw: nil, content: (text: input, image: nil), choices: nil))
               self.logs.append((id: nil, from: first.name, to: nil, group: time, raw: nil, content: (text: content.joined(separator: "\n"), image: nil), choices: nil))
               self.choices.removeAll()
               
               for var sequence in newSequences {
                  sequence.append(.completion)
                  
                  Script.shared.queue.append((first.name, sequence))
               }
            } else if fallback {
               self.choices.removeAll()
               
               await Script.shared.run(name: first.name, sequences: tempSequences, words: [])
            }
         } else {
            for i in 0..<sequences.count {
               await Script.shared.run(name: sequences[i].0, sequences: [sequences[i].3], words: []) { x in
                  var y = x
                  var content = [String]()
                  let choices: [String]?
                  
                  for sequence in x {
                     for step in sequence {
                        if case .message(let message) = step {
                           content.append(message.reduce(into: String(), { content, inline in
                              if inline.attributes == nil {
                                 content.append(inline.text)
                              } else {
                                 content.append(inline.text.filter { !$0.isNewline })
                              }
                           }))
                        }
                     }
                  }
                  
                  y.append(Sequence(name: String()))
                  
                  if let c = sequences[i].5 {
                     choices = c.reduce(into: [String](), { x, y in
                        x.append(y.0)
                     })
                     self.choices.removeAll()
                     self.choices.append(contentsOf: c)
                  } else {
                     choices = nil
                  }
                  
                  if i > 0 {
                     self.logs.append((id: sequences[i].1, from: sequences[i].0, to: sequences[0].0, group: time, raw: sequences[i].2, content: (text: content.joined(separator: "\n"), image: nil), choices: choices))
                  } else {
                     self.logs.append((id: nil, from: nil, to: sequences[i].0, group: time, raw: nil, content: (text: input, image: nil), choices: choices))
                     self.logs.append((id: sequences[i].1, from: sequences[i].0, to: nil, group: time, raw: sequences[i].2, content: (text: content.joined(separator: "\n"), image: nil), choices: choices))
                  }
                  
                  if let likability = sequences[i].4 {
                     withAnimation {
                        self.likability = likability
                     }
                  }
                  
                  return y
               }
            }
         }
         
         while self.logs.count > 10 {
            let group = self.logs[0].group
            
            for i in stride(from: self.logs.count - 1, through: 0, by: -1) {
               if self.logs[i].group == group {
                  self.logs.remove(at: i)
               }
            }
         }
      }
   }
   
   private func talk(image: CGImage, temperature: Double, multiple: Bool, mute: Bool) async {
      var queue = Script.shared.characters
      
      if let first = queue.first {
         let time = CACurrentMediaTime()
         var sequences = [(String, UUID?, String, Sequence, Double?, [(String, URL?)]?)]()
         
         if multiple {
            queue.removeFirst()
         } else {
            queue.removeAll()
         }
         
         if var prompt = first.prompt {
            withAnimation(.easeOut(duration: 0.5)) {
               self.isLoading = true
            }
            
            if let memory = (await Task.detached {
               if let data = self.load() {
                  return String(data: data, encoding: .utf8)
               }
               
               return nil
            }.value) {
               prompt.append("\n\(memory)")
            }
            
            var messages: [[String: Any]] = [["role": "system", "content": prompt]]
            var i = self.logs.count - 1
            
            while i > 0 {
               if self.logs[i].from == first.name && self.logs[i].to == nil && self.logs[i - 1].from == nil && self.logs[i - 1].to == first.name {
                  var parts = [[String: Any]]()
                  
                  if let text = self.logs[i - 1].content.text {
                     parts.append(["type": "text", "text": text])
                  }
                  
                  if let image = self.logs[i - 1].content.image {
                     if let dataURL = (await Task.detached {
                        var dataURL: String? = nil
                        
                        if let resizedImage = self.resize(image: image) {
                           dataURL = self.convert(image: resizedImage)
                        }
                        
                        return dataURL
                     }.value) {
                        parts.append(["type": "image", "image": dataURL])
                     }
                  }
                  
                  if !parts.isEmpty {
                     if let raw = self.logs[i].raw {
                        messages.insert(["role": "assistant", "content": raw], at: 1)
                        messages.insert(["role": "user", "content": parts], at: 1)
                     } else if let text = self.logs[i].content.text {
                        messages.insert(["role": "assistant", "content": text], at: 1)
                        messages.insert(["role": "user", "content": parts], at: 1)
                     }
                  }
                  
                  i -= 2
               } else {
                  i -= 1
               }
            }
            
            if let dataURL = (await Task.detached {
               var dataURL: String? = nil
               
               if let resizedImage = self.resize(image: image) {
                  dataURL = self.convert(image: resizedImage)
               }
               
               return dataURL
            }.value) {
               messages.append(["role": "user", "content": [["type": "image", "image": dataURL]]])
            } else {
               withAnimation(.easeIn(duration: 0.5)) {
                  self.isLoading = false
               }
               
               return
            }
            
            if let (output, content, likability, terms, state, choices, memory, voice) = await self.generate(messages: messages, voice: mute ? nil : await self.sample(path: first.path, sequences: first.sequences), language: first.language, temperature: temperature) {
               var sequence = Sequence(name: "Activate", state: nil)
               let id = UUID()
               var candidates = terms.reduce(into: [(target: String, words: [Word])]()) { output, value in
                  let term = value.filter { !$0.isEmpty }
                  
                  guard let word = term.last else {
                     return
                  }
                  
                  if term.count == 1 {
                     output.append((target: word, words: [Word(name: word, attributes: nil)]))
                  } else {
                     for index in 0..<term.count - 1 {
                        let parts = Array(term[index...])
                        let separator = parts.allSatisfy { $0.allSatisfy { $0.isASCII } } ? String("\u{0020}") : String()
                        
                        output.append((target: parts.joined(separator: separator), words: parts.enumerated().map { Word(name: $0.element, attributes: $0.offset < parts.count - 1 ? [] : nil) }))
                     }
                  }
               }
               
               candidates.sort { $0.target.count > $1.target.count }
               
               var inlines: [(text: String, attributes: [String]?)] = content.isEmpty ? [] : [(content, nil)]
               var words = [Word]()
               
               for candidate in candidates {
                  inlines = inlines.reduce(into: []) { output, inline in
                     guard inline.attributes == nil else {
                        output.append(inline)
                        
                        return
                     }
                     
                     var text = inline.text
                     
                     while let range = text.range(of: candidate.target, options: .caseInsensitive) {
                        if range.lowerBound != text.startIndex {
                           output.append((text: String(text[..<range.lowerBound]), attributes: nil))
                        }
                        
                        var inline = String(text[range])
                        
                        if candidate.words.count > 1, let lastWord = candidate.words.last?.name, let lastRange = inline.range(of: lastWord, options: [.caseInsensitive, .backwards]) {
                           inline.insert("\n", at: lastRange.lowerBound)
                        }
                        
                        output.append((text: inline, attributes: []))
                        
                        for word in candidate.words {
                           if !words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) && !Script.shared.words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) {
                              words.append(word)
                           }
                        }
                        
                        text = String(text[range.upperBound...])
                     }
                     
                     if !text.isEmpty {
                        output.append((text: text, attributes: nil))
                     }
                  }
               }
               
               sequence.append(.message(Message(id: id, inlines: inlines)))
               
               if let voice {
                  sequence.append(.audio(voice))
               }
               
               sequence.append(.sequence(Sequence(name: "Emote", state: state ?? String())))
               sequences.append((first.name, id, output, sequence, likability, choices))
               
               if !words.isEmpty {
                  self.discoveries.append(contentsOf: words)
               }
               
               if let memory {
                  await Task.detached {
                     if let data = memory.data(using: .utf8) {
                        self.save(data)
                     }
                  }.value
               }
               
               while !queue.isEmpty {
                  let character = queue.removeFirst()
                  
                  if var prompt = character.prompt {
                     if let memory = (await Task.detached {
                        if let data = self.load() {
                           return String(data: data, encoding: .utf8)
                        }
                        
                        return nil
                     }.value) {
                        prompt.append("\n\(memory)")
                     }
                     
                     if let (output, content, _, terms, state, _, memory, voice) = await self.generate(messages: [["role": "system", "content": prompt], ["role": "user", "content": [["type": "text", "text": content]]]], voice: mute ? nil : self.sample(path: character.path, sequences: character.sequences), language: character.language, temperature: temperature) {
                        var sequence = Sequence(name: "Activate", state: nil)
                        let id = UUID()
                        var candidates = terms.reduce(into: [(target: String, words: [Word])]()) { output, value in
                           let term = value.filter { !$0.isEmpty }
                           
                           guard let word = term.last else {
                              return
                           }
                           
                           if term.count == 1 {
                              output.append((target: word, words: [Word(name: word, attributes: nil)]))
                           } else {
                              for index in 0..<term.count - 1 {
                                 let parts = Array(term[index...])
                                 let separator = parts.allSatisfy { $0.allSatisfy { $0.isASCII } } ? String("\u{0020}") : String()
                                 
                                 output.append((target: parts.joined(separator: separator), words: parts.enumerated().map { Word(name: $0.element, attributes: $0.offset < parts.count - 1 ? [] : nil) }))
                              }
                           }
                        }
                        
                        candidates.sort { $0.target.count > $1.target.count }
                        
                        var inlines: [(text: String, attributes: [String]?)] = content.isEmpty ? [] : [(content, nil)]
                        var words = [Word]()
                        
                        for candidate in candidates {
                           inlines = inlines.reduce(into: []) { output, inline in
                              guard inline.attributes == nil else {
                                 output.append(inline)
                                 
                                 return
                              }
                              
                              var text = inline.text
                              
                              while let range = text.range(of: candidate.target, options: .caseInsensitive) {
                                 if range.lowerBound != text.startIndex {
                                    output.append((text: String(text[..<range.lowerBound]), attributes: nil))
                                 }
                                 
                                 var inline = String(text[range])
                                 
                                 if candidate.words.count > 1, let lastWord = candidate.words.last?.name, let lastRange = inline.range(of: lastWord, options: [.caseInsensitive, .backwards]) {
                                    inline.insert("\n", at: lastRange.lowerBound)
                                 }
                                 
                                 output.append((text: inline, attributes: []))
                                 
                                 for word in candidate.words {
                                    if !words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) && !Script.shared.words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) {
                                       words.append(word)
                                    }
                                 }
                                 
                                 text = String(text[range.upperBound...])
                              }
                              
                              if !text.isEmpty {
                                 output.append((text: text, attributes: nil))
                              }
                           }
                        }
                        
                        sequence.append(.message(Message(id: id, inlines: inlines)))
                        
                        if let voice {
                           sequence.append(.audio(voice))
                        }
                        
                        sequence.append(.sequence(Sequence(name: "Emote", state: state ?? String())))
                        sequences.append((character.name, id, output, sequence, nil, nil))
                        
                        if !words.isEmpty {
                           self.discoveries.append(contentsOf: words)
                        }
                        
                        if let memory {
                           await Task.detached {
                              if let data = memory.data(using: .utf8) {
                                 self.save(data)
                              }
                           }.value
                        }
                     } else {
                        break
                     }
                  } else {
                     break
                  }
               }
            }
            
            withAnimation(.easeIn(duration: 0.5)) {
               self.isLoading = false
            }
         }
         
         for i in 0..<sequences.count {
            await Script.shared.run(name: sequences[i].0, sequences: [sequences[i].3], words: []) { x in
               var y = x
               var content = [String]()
               let choices: [String]?
               
               for sequence in x {
                  for step in sequence {
                     if case .message(let message) = step {
                        content.append(message.reduce(into: String(), { content, inline in
                           if inline.attributes == nil {
                              content.append(inline.text)
                           } else {
                              content.append(inline.text.filter { !$0.isNewline })
                           }
                        }))
                     }
                  }
               }
               
               y.append(Sequence(name: String()))
               
               if let c = sequences[i].5 {
                  choices = c.reduce(into: [String](), { x, y in
                     x.append(y.0)
                  })
                  self.choices.removeAll()
                  self.choices.append(contentsOf: c)
               } else {
                  choices = nil
               }
               
               if i > 0 {
                  self.logs.append((id: sequences[i].1, from: sequences[i].0, to: sequences[0].0, group: time, raw: sequences[i].2, content: (text: content.joined(separator: "\n"), image: nil), choices: choices))
               } else {
                  var index = self.logs.count - 1
                  
                  while index >= 0 {
                     if self.logs[index].content.image != nil {
                        let group = self.logs[index].group
                        
                        for j in stride(from: self.logs.count - 1, through: 0, by: -1) {
                           if self.logs[j].group == group {
                              self.logs.remove(at: j)
                           }
                        }
                        
                        index = self.logs.count - 1
                        
                        continue
                     }
                     
                     index -= 1
                  }
                  
                  self.logs.append((id: nil, from: nil, to: sequences[i].0, group: time, raw: nil, content: (text: nil, image: image), choices: choices))
                  self.logs.append((id: sequences[i].1, from: sequences[i].0, to: nil, group: time, raw: sequences[i].2, content: (text: content.joined(separator: "\n"), image: nil), choices: choices))
               }
               
               if let likability = sequences[i].4 {
                  withAnimation {
                     self.likability = likability
                  }
               }
               
               return y
            }
         }
         
         while self.logs.count > 10 {
            let group = self.logs[0].group
            
            for i in stride(from: self.logs.count - 1, through: 0, by: -1) {
               if self.logs[i].group == group {
                  self.logs.remove(at: i)
               }
            }
         }
      }
   }
   
   private nonisolated func load(from filename: String = "MEMORY.md") -> Data? {
      if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
         let path = containerUrl.appendingPathComponent("Documents/\(filename)").path(percentEncoded: false)
         
         if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
            defer {
               try? file.close()
            }
            
            return try? file.readToEnd()
         }
      }
      
      return nil
   }
   
   private nonisolated func save(_ data: Data, to filename: String = "MEMORY.md") {
      if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
         let path = containerUrl.appendingPathComponent("Documents/\(filename)").path(percentEncoded: false)
         
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
   }
   
   private func sample(path: String, sequences: [Sequence]) async -> Data? {
      return await Task.detached {
         var sequenceQueue = sequences
         
         while !sequenceQueue.isEmpty {
            let sequence = sequenceQueue.removeFirst()
            var index: Int? = nil
            
            for (i, step) in sequence.enumerated() {
               if case .sequence(let s) = step {
                  sequenceQueue.append(s)
               } else if case .message = step {
                  index = i
               } else if case .sound(let sound) = step, i - 1 == index, let soundPath = sound.path {
                  let path = URL(filePath: path).deletingLastPathComponent().appending(path: soundPath, directoryHint: .inferFromPath).path(percentEncoded: false)
                  
                  if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                     defer {
                        try? file.close()
                     }
                     
                     if let data = try? file.readToEnd(), data.count > 44, let riff = String(data: data[0..<4], encoding: .ascii), riff == "RIFF", let wave = String(data: data[8..<12], encoding: .ascii), wave == "WAVE" && String(data: data[12..<16], encoding: .ascii) == "fmt " {
                        let sampleRate = data.subdata(in: 24..<28).withUnsafeBytes { $0.load(as: UInt32.self) }
                        let channels = data.subdata(in: 22..<24).withUnsafeBytes { $0.load(as: UInt16.self) }
                        let bitsPerSample = data.subdata(in: 34..<36).withUnsafeBytes { $0.load(as: UInt16.self) }
                        var dataChunkOffset = 36
                        
                        while dataChunkOffset + 8 < data.count {
                           let chunkID = String(data: data[dataChunkOffset..<dataChunkOffset + 4], encoding: .ascii)
                           let chunkSize = data.subdata(in: dataChunkOffset + 4..<dataChunkOffset + 8).withUnsafeBytes { $0.load(as: UInt32.self) }
                           
                           if chunkID == "data" {
                              let duration = Double(Int(chunkSize) / Int(bitsPerSample / 8 * channels)) / Double(sampleRate)
                              
                              if duration > 3.0 && duration <= 10.0 {
                                 return data
                              }
                              
                              break
                           }
                           
                           dataChunkOffset += 8 + Int(chunkSize)
                        }
                     }
                  }
               }
            }
         }
         
         return nil
      }.value
   }
   
   private func generate(messages: [[String: Any]], voice: Data?, language: String?, temperature: Double) async -> (String, String, Double?, [[String]], String?, [(String, URL?)], String?, Data?)? {
      if let data = try? JSONSerialization.data(withJSONObject: ["messages": messages, "temperature": round(temperature * 10.0) / 10.0]) {
         var request = URLRequest(url: URL(string: "https://milchchan.com/api/generate")!)
         
         request.httpMethod = "POST"
         request.setValue("application/json", forHTTPHeaderField: "Content-Type")
         request.httpBody = data
         request.timeoutInterval = 60.0
         
         if let (data, response) = try? await URLSession.shared.data(for: request), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "application/json", let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [String: Any], let content = jsonRoot["content"] as? String {
            var likability: Double? = nil
            var terms = [[String]]()
            var state: String? = nil
            var choices = [(String, URL?)]()
            var memory: String? = nil
            var wave: Data? = nil
            
            if let value = jsonRoot["likability"] as? Double {
               likability = value
            }
            
            if let value = jsonRoot["terms"] as? [Any] {
               for item in value {
                  if let s = item as? String {
                     terms.append([s])
                  } else if let a = item as? [String] {
                     terms.append(a)
                  }
               }
            }
            
            if let states = jsonRoot["states"] as? [String: Any] {
               var max = 0.0
               
               for (key, object) in states {
                  if let value = object as? Double, value > max {
                     state = key
                     max = value
                  }
               }
            }
            
            if let objects = jsonRoot["choices"] as? [Any] {
               for object in objects {
                  if let value = object as? String {
                     choices.append((value, nil))
                  } else if let dictionary = object as? [String: Any?] {
                     if let text = dictionary["text"] as? String {
                        if let value = dictionary["url"] as? String {
                           if value.lowercased().hasPrefix("https://"), let url = URL(string: value) {
                              choices.append((text, url))
                           }
                        } else {
                           choices.append((text, nil))
                        }
                     }
                  }
               }
            }
            
            if let value = jsonRoot["memory"] as? String {
               memory = value
            }
            
            if let voice, let language {
               wave = await self.generate(prompt: voice, input: content, language: language, temperature: temperature)
            }
            
            if let data = try? JSONSerialization.data(withJSONObject: jsonRoot, options: .prettyPrinted), let output = String(data: data, encoding: .utf8) {
               return (output, content, likability, terms, state, choices, memory, wave)
            }
         }
      }
      
      return nil
   }
   
   private func generate(prompt: Data, input: String, language: String, temperature: Double) async -> Data? {
      if let data = try? JSONSerialization.data(withJSONObject: ["input": input, "language": language, "temperature": round(temperature * 10.0) / 10.0]) {
         let request = await Task.detached {
            var request = URLRequest(url: URL(string: "https://milchchan.com/api/generate")!)
            let boundary = UUID().uuidString
            var body = Data()
            
            body.append("--\(boundary)\r\n".data(using: .utf8)!)
            body.append("Content-Disposition: form-data; name=\"file\"; filename=\"prompt.wav\"\r\n".data(using: .utf8)!)
            body.append("Content-Type: audio/wav\r\n".data(using: .utf8)!)
            body.append("Content-Transfer-Encoding: binary\r\n\r\n".data(using: .utf8)!)
            body.append(prompt)
            body.append("\r\n".data(using: .utf8)!)
            body.append("--\(boundary)\r\n".data(using: .utf8)!)
            body.append("Content-Disposition: form-data; name=\"data\"\r\n".data(using: .utf8)!)
            body.append("Content-Type: application/json\r\n\r\n".data(using: .utf8)!)
            body.append(data)
            body.append("\r\n".data(using: .utf8)!)
            body.append("--\(boundary)--\r\n".data(using: .utf8)!)
            
            request.httpMethod = "POST"
            request.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
            request.httpBody = body
            request.timeoutInterval = 60.0
            
            return request
         }.value
         
         if let (data, response) = try? await URLSession.shared.data(for: request), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "audio/wav" {
            return data
         }
      }
      
      return nil
   }
      
   private func startRecognize() {
      guard let recognizer = SFSpeechRecognizer() else {
         return
      }
      
      withAnimation(.linear(duration: 0.5)) {
         self.isRecording = true
      }
      
      let message = self.message
      
      self.speechRecognizer = recognizer
      
      Task {
         let speechAllowed: Bool
         
         let authorizationStatus = SFSpeechRecognizer.authorizationStatus()
         
         if authorizationStatus == .notDetermined {
            speechAllowed = await withCheckedContinuation(isolation: nil) { @Sendable (continuation: CheckedContinuation<Bool, Never>) in
               SFSpeechRecognizer.requestAuthorization { status in
                  continuation.resume(returning: status == .authorized)
               }
            }
         } else {
            speechAllowed = authorizationStatus == .authorized
         }
         
         guard self.isRecording, self.speechRecognizer === recognizer else {
            return
         }
         
         guard speechAllowed else {
            self.speechRecognizer = nil
            self.isRecording = false
            
            return
         }
         
         let microphoneAllowed = await AVAudioApplication.requestRecordPermission()
         
         guard self.isRecording, self.speechRecognizer === recognizer else {
            return
         }
         
         guard microphoneAllowed else {
            self.speechRecognizer = nil
            self.isRecording = false
            
            return
         }
         
         let audioSessionActivated: Bool
         do {
            let audioSession = AVAudioSession.sharedInstance()
            
            if audioSession.category != .playAndRecord || audioSession.mode != .measurement {
               try audioSession.setCategory(.playAndRecord, mode: .measurement, options: .duckOthers)
            }
            
            try audioSession.setActive(true, options: .notifyOthersOnDeactivation)
            audioSessionActivated = true
         } catch {
            audioSessionActivated = false
         }
         
         guard self.isRecording, self.speechRecognizer === recognizer else {
            return
         }
         
         guard audioSessionActivated && recognizer.isAvailable else {
            self.speechRecognizer = nil
            self.isRecording = false
            
            return
         }
         
         if let previousAudioEngine = self.audioEngine {
            if previousAudioEngine.isRunning {
               previousAudioEngine.stop()
            }
            
            previousAudioEngine.inputNode.removeTap(onBus: 0)
            self.audioEngine = nil
         }
         
         let audioEngine = AVAudioEngine()
         let inputNode = audioEngine.inputNode
         let inputFormat = inputNode.outputFormat(forBus: 0)
         
         guard inputFormat.sampleRate > 0.0, inputFormat.channelCount > 0 else {
            self.speechRecognizer = nil
            
            withAnimation(.linear(duration: 0.5)) {
               self.isRecording = false
               self.volumeLevel = 0.0
            }
            
            return
         }
         
         let request = SFSpeechAudioBufferRecognitionRequest()
         
         request.shouldReportPartialResults = true
         
         self.installTap(on: inputNode, format: inputFormat, request: request) { level, duration in
            let multiplier = level > self.volumeLevel ? 5.0 : 10.0
            
            withAnimation(.linear(duration: duration * multiplier)) {
               self.volumeLevel = level
            }
         }
         
         audioEngine.prepare()
         
         do {
            try audioEngine.start()
         } catch {
            self.speechRecognizer = nil
            
            withAnimation(.linear(duration: 0.5)) {
               self.isRecording = false
               self.volumeLevel = 0.0
            }
            
            return
         }
         
         self.audioEngine = audioEngine
         self.speechAudioBufferRecognitionRequest = request
         self.speechRecognitionTask = recognizer.recognitionTask(with: request, resultHandler: { result, error in
            if error == nil {
               if let result {
                  let text = result.bestTranscription.formattedString
                  
                  if result.isFinal, self.audioEngine === audioEngine {
                     if audioEngine.isRunning {
                        audioEngine.stop()
                     }
                     
                     audioEngine.inputNode.removeTap(onBus: 0)
                     self.audioEngine = nil
                     
                     withAnimation(.linear(duration: 0.5)) {
                        self.isRecording = false
                        self.volumeLevel = 0.0
                     }
                  }
                  
                  if !text.isEmpty {
                     self.message = message + text
                  }
               }
            } else if self.audioEngine === audioEngine {
               if audioEngine.isRunning {
                  audioEngine.stop()
               }
               
               audioEngine.inputNode.removeTap(onBus: 0)
               self.audioEngine = nil
               
               withAnimation(.linear(duration: 0.5)) {
                  self.isRecording = false
                  self.volumeLevel = 0.0
               }
            }
         })
      }
   }
      
   private func stopRecognize() {
      self.speechRecognizer = nil
      
      if let speechRecognitionTask = self.speechRecognitionTask {
         speechRecognitionTask.cancel()
         self.speechRecognitionTask = nil
      }
      
      if let audioEngine = self.audioEngine {
         if audioEngine.isRunning {
            audioEngine.stop()
         }
         
         audioEngine.inputNode.removeTap(onBus: 0)
         
         self.audioEngine = nil
      }
      
      if let speechAudioBufferRecognitionRequest = self.speechAudioBufferRecognitionRequest {
         speechAudioBufferRecognitionRequest.endAudio()
         self.speechAudioBufferRecognitionRequest = nil
      }
      
      withAnimation(.linear(duration: 0.5)) {
         self.isRecording = false
         self.volumeLevel = 0.0
      }
   }
   
   private nonisolated func installTap(on inputNode: AVAudioInputNode, format: AVAudioFormat, request: SFSpeechAudioBufferRecognitionRequest, onChange: @escaping @MainActor (Double, Double) -> Void
   ) {
      inputNode.installTap(onBus: 0, bufferSize: 1024, format: format) { buffer, _ in
         guard buffer.frameLength > 0, buffer.stride > 0, buffer.format.sampleRate > 0.0 else {
            return
         }
         
         request.append(buffer)
         
         guard let floatChannelData = buffer.floatChannelData else {
            return
         }
         
         let pointee = floatChannelData.pointee
         var sum: Float = 0.0
         
         for i in stride(from: 0, to: Int(buffer.frameLength), by: buffer.stride) {
            sum += pointee[i] * pointee[i]
         }
         
         let rms = sqrt(sum / Float(buffer.frameLength))
         let dB = rms == 0.0 ? 0.0 : 20.0 * log10(rms)
         let minimum: Float = -50.0
         let maximum: Float = -25.0
         let level = Double(dB > maximum ? 1.0 : (abs(minimum) - abs(max(dB, minimum))) / (abs(minimum) - abs(maximum)))
         let duration = Double(buffer.frameLength) / buffer.format.sampleRate
         
         Task { @MainActor in
            onChange(level, duration)
         }
      }
   }
   
   private nonisolated func resize(image: CGImage, maximum: Double = 768) -> CGImage? {
      let imageWidth = Double(image.width)
      let imageHeight = Double(image.height)
      let width: Double
      let height: Double
      
      if imageWidth < imageHeight {
         if imageHeight > maximum {
            width = floor(maximum / imageHeight * imageWidth)
            height = maximum
         } else {
            width = imageWidth
            height = imageHeight
         }
      } else if imageWidth > maximum {
         width = maximum
         height = floor(maximum / imageWidth * imageHeight)
      } else {
         width = imageWidth
         height = imageHeight
      }
      
      let size = CGSize(width: width, height: height)
      let rendererFormat = UIGraphicsImageRendererFormat()
      
      rendererFormat.opaque = false
      rendererFormat.scale = 1.0
      rendererFormat.preferredRange = .standard
      
      let renderer = UIGraphicsImageRenderer(size: size, format: rendererFormat)
      let resizedImage = renderer.image { rendererContext in
         let context = rendererContext.cgContext
         
         context.interpolationQuality = .high
         context.setAllowsAntialiasing(true)
         context.clear(CGRect(x: 0.0, y: 0.0, width: width, height: height))
         context.translateBy(x: 0.0, y: height)
         context.scaleBy(x: 1.0, y: -1.0)
         context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: width, height: height))
      }.cgImage
      
      return resizedImage
   }
   
   private nonisolated func convert(image: CGImage) -> String? {
      let mutableData = NSMutableData()
      
      guard let destination = CGImageDestinationCreateWithData(mutableData, UTType.jpeg.identifier as CFString, 1, nil) else {
         return nil
      }
      
      CGImageDestinationAddImage(destination, image, [kCGImageDestinationLossyCompressionQuality: 0.75] as CFDictionary)
      
      guard CGImageDestinationFinalize(destination) else {
         return nil
      }
      
      return "data:image/jpeg;base64,\(mutableData.base64EncodedString(options: []))"
   }
   
   private func convert(from: String) -> UIColor {
      let scanner = Scanner(string: from)
      var c: UInt64 = 0
      
      scanner.charactersToBeSkipped = CharacterSet(charactersIn: "#")
      scanner.scanHexInt64(&c)
      
      let red = Double((c & 0xff0000) >> 16) / 255.0
      let green = Double((c & 0x00ff00) >> 8) / 255.0
      let blue = Double(c & 0x0000ff) / 255.0
      
      return UIColor(red: red, green: green, blue: blue, alpha: 1.0)
   }
}

struct Stage: UIViewRepresentable {
   @Binding var prompt: (String?, Word?, Bool, Set<Character>?, [(String, URL?)], Int, Double)
   @Binding var logs: [(id: UUID?, from: String?, to: String?, group: Double, raw: String?, content: (text: String?, image: CGImage?), choices: [String]?)]
   @Binding var resource: (old: String, new: String)
   @Binding var attributes: [String]
   @Binding var types: Int
   @Binding var labels: [String]
   @Binding var likability: Double?
   @Binding var choices: [(String, URL?)]
   @Binding var words: [Word]
   var active: Bool
   var pause: Bool
   @Binding var idle: Bool
   @Binding var changing: Bool
   @Binding var loading: Bool
   @Binding var discoveries: [Word]
   var temperature: Double
   var accent: UIColor
   var scale: Double
   var mute: Bool
   @State var permissions = Set<String>()
   
   init(prompt: Binding<(String?, Word?, Bool, Set<Character>?, [(String, URL?)], Int, Double)>, logs: Binding<[(id: UUID?, from: String?, to: String?, group: Double, raw: String?, content: (text: String?, image: CGImage?), choices: [String]?)]>, resource: Binding<(old: String, new: String)>, attributes: Binding<[String]>, types: Binding<Int>, labels: Binding<[String]>, likability: Binding<Double?>, choices: Binding<[(String, URL?)]>, words: Binding<[Word]>, active: Bool, pause: Bool, idle: Binding<Bool>, changing: Binding<Bool>, loading: Binding<Bool>, discoveries: Binding<[Word]>, temperature: Double, accent: UIColor, scale: Double, mute: Bool) {
      self._prompt = prompt
      self._logs = logs
      self._resource = resource
      self._attributes = attributes
      self._types = types
      self._labels = labels
      self._likability = likability
      self._choices = choices
      self._words = words
      self.active = active
      self.pause = pause
      self._idle = idle
      self._changing = changing
      self._loading = loading
      self._discoveries = discoveries
      self.temperature = temperature
      self.accent = accent
      self.scale = scale
      self.mute = mute
   }
   
   func makeUIView(context: Context) -> WallView {
      let wallView = WallView(frame: .zero)
      let agentView = AgentView(path: self.resource.old.isEmpty ? Double.random(in: 0..<1) < 0.5 ? "Milch" : "Merku" : self.resource.old, types: self.types, scale: self.scale, stars: self.words.count)
      
      agentView.accent = self.accent
      agentView.mute = self.mute
      
      context.coordinator.uiView = wallView
      context.coordinator.scale = self.scale
      agentView.translatesAutoresizingMaskIntoConstraints = false
      agentView.delegate = context.coordinator
      wallView.delegate = context.coordinator
      wallView.addSubview(agentView)
      wallView.addConstraint(NSLayoutConstraint(item: agentView, attribute: .centerX, relatedBy: .equal, toItem: wallView, attribute: .centerX, multiplier: 1.0, constant: 0.0))
      wallView.addConstraint(NSLayoutConstraint(item: agentView, attribute: .centerY, relatedBy: .equal, toItem: wallView, attribute: .centerY, multiplier: 1.0, constant: 0.0))
      wallView.addConstraint(NSLayoutConstraint(item: agentView, attribute: .width, relatedBy: .equal, toItem: wallView, attribute: .width, multiplier: 1.0, constant: 0.0))
      wallView.addConstraint(NSLayoutConstraint(item: agentView, attribute: .height, relatedBy: .equal, toItem: wallView, attribute: .height, multiplier: 1.0, constant: 0.0))
      
      Task {
         self.attributes.append(contentsOf: agentView.attributes)
         
         for characterView in agentView.characterViews {
            characterView.refresh()
            
            break
         }
      }
      
      return wallView
   }
   
   func updateUIView(_ uiView: WallView, context: Context) {
      Task {
         for view in uiView.subviews {
            if let agentView = view as? AgentView {
               var labelSet = Set<String>(self.labels)
               var isUpdated = false
               var flags = self.types
               var current = self.labels
               var types = agentView.types
               var typeIndex = -1
               
               if let first = agentView.characterViews.first {
                  if self.resource.old != self.resource.new {
                     self.resource.old = self.resource.new
                     
                     await Script.shared.run(name: first.name!, sequences: Script.shared.characters.reduce(into: [], { x, y in
                        if y.name == first.name {
                           for sequence in y.sequences {
                              if sequence.name == "Stop" {
                                 x.append(sequence)
                              }
                           }
                        }
                     }), words: []) { x in
                        var y = x
                        
                        y.append(Sequence(name: nil))
                        
                        Script.shared.queue.removeAll()
                        
                        return y
                     }
                  }
                  
                  if self.words != context.coordinator.words {
                     context.coordinator.words = self.words
                     agentView.update(stars: self.words.count)
                  }
                  
                  if !self.discoveries.isEmpty {
                     let words = self.discoveries
                     
                     self.discoveries.removeAll()
                     
                     Task.detached {
                        let image = UIImage(systemName: "plus", withConfiguration: UIImage.SymbolConfiguration(font: .systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .caption1).pointSize, weight: .bold)))!
                        var codes = [Int]()
                        var attempted = [Word]()
                        
                        await MainActor.run {
                           let word = words[Int.random(in: 0..<words.count)]
                           
                           agentView.notify(characterView: first, image: image, text: word.name, duration: 3.0) {
                              withAnimation {
                                 self.words.append(word)
                              }
                              
                              let storedWords = self.words
                              
                              Task.detached { @Sendable [storedWords] in
                                 let encoder = JSONEncoder()
                                 
                                 if let data = try? encoder.encode(storedWords) {
                                    if let url = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {
                                       let path = url.appending(path: "words.json", directoryHint: .inferFromPath).path(percentEncoded: false)
                                       
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
                                    
                                    if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
                                       let documentsUrl = containerUrl.appending(path: "Documents", directoryHint: .isDirectory)
                                       let documentsPath = documentsUrl.path(percentEncoded: false)
                                       let url = documentsUrl.appending(path: ".words.json", directoryHint: .inferFromPath)
                                       let path = url.path(percentEncoded: false)
                                       
                                       if !FileManager.default.fileExists(atPath: documentsPath) {
                                          try? FileManager.default.createDirectory(atPath: documentsPath, withIntermediateDirectories: false)
                                       }
                                       
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
                                       
                                       if let currentVersion = NSFileVersion.currentVersionOfItem(at: url), currentVersion.isConflict {
                                          try? NSFileVersion.removeOtherVersionsOfItem(at: url)
                                          
                                          if let conflictVersions = NSFileVersion.unresolvedConflictVersionsOfItem(at: url) {
                                             for fileVersion in conflictVersions {
                                                fileVersion.isResolved = true
                                             }
                                          }
                                       }
                                    }
                                 }
                              }
                           }
                        }
                        
                        for word in words {
                           if word.attributes == nil {
                              if let data = try? await JSONSerialization.data(withJSONObject: ["name": word.name, "language": first.language as Any]) {
                                 var request = URLRequest(url: URL(string: "https://milchchan.com/api/word")!)
                                 
                                 request.httpMethod = "POST"
                                 request.setValue("application/json", forHTTPHeaderField: "Content-Type")
                                 request.httpBody = data
                                 
                                 if let (_, response) = try? await URLSession.shared.data(for: request), let httpResponse = response as? HTTPURLResponse {
                                    codes.append(httpResponse.statusCode)
                                 }
                              }
                              
                              attempted.append(word)
                           }
                        }
                        
                        if codes.count < attempted.count || codes.contains(where: { $0 != 201 }) {
                           Task.detached {
                              let image = UIImage(systemName: "exclamationmark.icloud", withConfiguration: UIImage.SymbolConfiguration(font: .systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .caption1).pointSize, weight: .bold)))!.applyingSymbolConfiguration(.preferringMonochrome())!
                              
                              await MainActor.run {
                                 agentView.notify(characterView: first, image: image, text: nil, duration: 3.0)
                              }
                           }
                        }
                     }
                  }
               }
               
               for i in 0..<current.count {
                  if flags & Int(pow(2.0, Double(i))) > 0 {
                     if types.contains(where: { !$0.1 && $0.0 == current[i] }) {
                        for tuple in agentView.toggle(type: current[i]) {
                           for j in 0..<types.count {
                              if types[j].0 == tuple.0 {
                                 types[j].1 = tuple.1
                                 
                                 break
                              }
                           }
                           
                           for j in 0..<current.count {
                              if current[j] == tuple.0 {
                                 if tuple.1 {
                                    flags = flags | Int(pow(2.0, Double(j)))
                                 } else {
                                    var x = 0
                                    
                                    for k in 0..<current.count {
                                       if j != k && flags & Int(pow(2.0, Double(k))) > 0 {
                                          x = x | Int(pow(2.0, Double(k)))
                                       }
                                    }
                                    
                                    flags = flags & x
                                 }
                                 
                                 break
                              }
                           }
                        }
                        
                        isUpdated = true
                     }
                  } else if types.contains(where: { $0.1 && $0.0 == current[i] }) {
                     var x = 0
                     
                     agentView.toggle(type: current[i])
                     
                     for j in 0..<current.count {
                        if i != j && flags & Int(pow(2.0, Double(j))) > 0 {
                           x = x | Int(pow(2.0, Double(j)))
                        }
                     }
                     
                     flags = flags & x
                     isUpdated = true
                  }
               }
               
               for i in 0..<types.count {
                  let type = types[i].0
                  
                  if self.permissions.contains(type) {
                     if i < current.count {
                        if current[i] != type {
                           current[i] = type
                           isUpdated = true
                        }
                     } else if !labelSet.contains(type) {
                        current.append(type)
                        isUpdated = true
                        labelSet.insert(type)
                     }
                     
                     typeIndex += 1
                  }
               }
               
               if current.count - 1 > typeIndex {
                  for i in stride(from: current.count - 1, to: typeIndex, by: -1) {
                     var x = 0
                     
                     for j in 0..<current.count {
                        if i != j && flags & Int(pow(2.0, Double(j))) > 0 {
                           x = x | Int(pow(2.0, Double(j)))
                        }
                     }
                     
                     flags = flags & x
                     current.remove(at: i)
                     isUpdated = true
                  }
               }
               
               if isUpdated {
                  withAnimation(.linear(duration: 0.5)) {
                     self.types = flags
                     
                     for i in 0..<current.count {
                        if i < self.labels.count {
                           if self.labels[i] != current[i] {
                              self.labels[i] = current[i]
                           }
                        } else {
                           self.labels.append(current[i])
                        }
                     }
                     
                     for i in stride(from: self.labels.count - 1, through: current.count, by: -1) {
                        self.labels.remove(at: i)
                     }
                  }
               }
               
               if self.temperature != context.coordinator.temperature {
                  context.coordinator.temperature = self.temperature
               }
               
               if self.accent != agentView.accent {
                  agentView.accent = self.accent
               }
               
               if self.scale != context.coordinator.scale {
                  agentView.change(scale: self.scale)
                  context.coordinator.scale = self.scale
               }
               
               if self.active != context.coordinator.active {
                  uiView.running = self.active
                  context.coordinator.active = self.active
               }
               
               if self.pause != context.coordinator.pause {
                  context.coordinator.pause = self.pause
               }
               
               if self.mute != agentView.mute {
                  agentView.mute = self.mute
               }
               
               break
            }
         }
      }
   }
   
   func makeCoordinator() -> Coordinator {
      return Coordinator(self)
   }
   
   class Coordinator: NSObject, AgentDelegate, WallDelegate {
      var uiView: WallView? = nil
      var words = [Word]()
      var scale = 1.0
      var temperature = 1.0
      var active = false
      var pause = false
      private var parent: Stage
      private var snapshot: (name: String?, types: [String]) = (name: nil, types: [])
      private var lines = [(text: String, attributes: [(name: String?, start: Int, end: Int)], timestamp: Date)]()
      private var timestamp: Date? = nil
      
      init(_ parent: Stage) {
         self.parent = parent
      }
      
      func agentShouldIdle(_ agent: AgentView, by name: String) -> Bool {
         if self.parent.loading || !self.active || self.pause || agent.characterViews.contains(where: { $0.name != name && !$0.balloonView!.isHidden }) || Double.random(in: 0.0..<1.0) < 0.5 {
            return true
         }
         
         let safeBounds = agent.bounds.inset(by: agent.safeAreaInsets)
         let multiple = safeBounds.width > safeBounds.height
         
         if multiple || agent.characterViews.firstIndex(where: { $0.name == name }) == 0 {
            Task {
               var fallback = false
               
               if self.parent.words.isEmpty {
                  fallback = true
               } else {
                  let samples = 10
                  var words = [Word]()
                  
                  for _ in 0..<samples {
                     let word = self.parent.words[Int.random(in: 0..<self.parent.words.count)]
                     
                     if let attributes = word.attributes {
                        if !attributes.isEmpty {
                           words.append(word)
                        }
                     } else {
                        words.append(word)
                     }
                  }
                  
                  if words.isEmpty {
                     fallback = true
                  } else if !(await self.talk(word: words[Int.random(in: 0..<words.count)], temperature: self.temperature, multiple: multiple, mute: self.parent.mute)) {
                     fallback = true
                  }
               }
               
               if fallback {
                  await Script.shared.run(name: name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                     if y.name == name {
                        for sequence in y.sequences {
                           if sequence.name == "Activate" {
                              x.append(sequence)
                           }
                        }
                     }
                  }), words: []) { x in
                     var y = x
                     
                     y.append(Sequence(name: String()))
                     
                     return y
                  }
               }
            }
         }
         
         return false
      }
      
      func agentWillSpeak(_ agent: AgentView, message: Message) {
         let nowDate = Date()
         var newLines = [(text: String(), attributes: [(name: String?, start: Int, end: Int)](), timestamp: nowDate)]
         
         for inline in message {
            if let names = inline.attributes {
               var term = String()
               var modifier = String()
               
               for character in inline.text {
                  if character.isNewline {
                     modifier.append(contentsOf: term)
                     term.removeAll()
                  } else {
                     term.append(character)
                  }
               }
               
               let text = modifier + term
               
               if text.isEmpty {
                  continue
               }
               
               let lineIndex = newLines.count - 1
               let start = newLines[lineIndex].text.count
               let modifierEnd = start + modifier.count
               let end = start + text.count
               
               newLines[lineIndex].text.append(contentsOf: text)
               
               if !modifier.isEmpty {
                  newLines[lineIndex].attributes.append((name: nil, start: start, end: modifierEnd))
               }
               
               if names.isEmpty {
                  newLines[lineIndex].attributes.append((name: nil, start: start, end: end))
               } else {
                  for name in names {
                     newLines[lineIndex].attributes.append((name: name, start: start, end: end))
                  }
               }
            } else {
               for character in inline.text {
                  if character.isNewline {
                     if !newLines.last!.text.isEmpty {
                        newLines.append((text: String(), attributes: [], timestamp: nowDate))
                     }
                  } else {
                     newLines[newLines.count - 1].text.append(character)
                  }
               }
            }
         }
         
         if newLines.last!.text.isEmpty {
            newLines.removeLast()
         }
         
         if !newLines.isEmpty {
            let index = self.lines.firstIndex { $0.timestamp < nowDate } ?? self.lines.endIndex
            
            self.lines.insert(contentsOf: newLines, at: index)
            
            if let uiView = self.uiView {
               let maxLines = max(Int(round(min(uiView.bounds.width, uiView.bounds.height) / ceil(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .body).pointSize * 1.5))), 1)
               
               if self.lines.count > maxLines {
                  self.lines.removeSubrange(maxLines..<self.lines.count)
               }
               
               var lines = self.lines
               let start = Int.random(in: (lines.count < maxLines / 2 ? min(lines.count, index + newLines.count) : maxLines / 2)...lines.count)
               
               if start < lines.count {
                  lines.removeSubrange(start..<lines.count)
               }
               
               uiView.reload(lines: lines.map { line in
                  var attributes = [(start: Int, end: Int)]()
                  var indexByStart = [Int: Int]()
                  
                  for attribute in line.attributes {
                     if let index = indexByStart[attribute.start] {
                        attributes[index].end = max(attributes[index].end, attribute.end)
                     } else {
                        indexByStart[attribute.start] = attributes.count
                        attributes.append((start: attribute.start, end: attribute.end))
                     }
                  }
                  
                  return (text: line.text, attributes: attributes)
               })
            }
         }
      }
      
      func agentDidStart(_ agent: AgentView) {
         let safeBounds = agent.bounds.inset(by: agent.safeAreaInsets)
         
         Task {
            await self.talk(word: nil, temperature: self.temperature, multiple: safeBounds.width > safeBounds.height, mute: self.parent.mute)
         }
      }
      
      func agentDidRender(_ agent: AgentView, image: CGImage, by name: String) {
         if let uiView = self.uiView {
            var typeSet = Set<String>()
            
            for characterView in agent.characterViews {
               if characterView.name == name {
                  let types = characterView.types.compactMap({ $0.value.1 ? $0.key : nil })
                  
                  if characterView.name != self.snapshot.name || !types.elementsEqual(self.snapshot.types) {
                     if characterView.fades.contains(where: { $0.value > 0.0 && $0.value < 1.0 }) {
                        let (i, _) = characterView.preview(timelines: characterView.cachedTimelines, images: &characterView.cachedImages)
                        
                        if let i {
                           self.snapshot.name = characterView.name
                           self.snapshot.types.removeAll()
                           self.snapshot.types.append(contentsOf: types)
                           
                           Task {
                              await uiView.reload(image: i)
                           }
                        }
                     } else {
                        self.snapshot.name = characterView.name
                        self.snapshot.types.removeAll()
                        self.snapshot.types.append(contentsOf: types)
                        
                        Task {
                           await uiView.reload(image: image)
                        }
                     }
                  }
               }
               
               for timeline in characterView.cachedTimelines {
                  if let type = timeline.animation.type, !typeSet.contains(type) {
                     typeSet.insert(type)
                  }
               }
            }
            
            if !self.parent.permissions.symmetricDifference(typeSet).isEmpty {
               self.parent.permissions = typeSet
            }
            
            if let timestamp = self.timestamp, Date().timeIntervalSince(timestamp) >= 60.0 {
               self.timestamp = nil
               
               Task {
                  await uiView.reload(frames: [], particles: 0)
               }
            }
         }
      }
      
      func agentDidRefresh(_ agent: AgentView) {
         if let characterView = agent.characterViews.first {
            let language = characterView.language
            
            Task {
               let scores = Script.shared.scores
               let knownWords = Script.shared.words
               let words = await Task.detached { @Sendable [scores, knownWords, language] in
                  let yesterday = Date(timeIntervalSinceNow: -60 * 60 * 24)
                  let epsilon: Double = 1e-6
                  var mean = 0.0
                  var data = [String: Double]()
                  var words = [Word]()
                  
                  for (key, value) in scores {
                     if value.3 > yesterday && value.1 > epsilon {
                        mean += value.1
                        data[key] = value.1
                     }
                  }
                  
                  if data.count > 0 {
                     var variance = 0.0
                     
                     mean /= Double(data.count)
                     
                     for (_, x) in data {
                        variance += (x - mean) * (x - mean)
                     }
                     
                     variance /= Double(data.count)
                     
                     if variance > 0.0 {
                        if let language {
                           let locale = Locale(identifier: "en_US_POSIX")
                           
                           for (key, value) in scores {
                              if let x = data[key], (x - mean) / variance >= 2.0 && !knownWords.contains(where: { $0.name.folding(options: [.caseInsensitive], locale: locale) == key }) {
                                 if let languages = value.2 {
                                    if languages.contains(language) {
                                       words.append(Word(name: value.0, attributes: nil))
                                    }
                                 } else {
                                    words.append(Word(name: value.0, attributes: nil))
                                 }
                              }
                           }
                        } else {
                           let locale = Locale(identifier: "en_US_POSIX")
                           
                           for (key, value) in scores {
                              if let x = data[key], (x - mean) / variance >= 2.0 && !knownWords.contains(where: { $0.name.folding(options: [.caseInsensitive], locale: locale) == key }) {
                                 words.append(Word(name: value.0, attributes: nil))
                              }
                           }
                        }
                     }
                  }
                  
                  return words
               }.value
               
               if !words.isEmpty {
                  let image = await Task.detached { UIImage(systemName: "plus", withConfiguration: UIImage.SymbolConfiguration(font: .systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .caption1).pointSize, weight: .bold)))! }.value
                  let word = words[Int.random(in: 0..<words.count)]
                  
                  agent.notify(characterView: characterView, image: image, text: word.name, duration: 3.0) { [weak self] in
                     guard let self else {
                        return
                     }
                     
                     withAnimation {
                        self.parent.words.append(word)
                     }
                     
                     let storedWords = self.parent.words
                     
                     Task.detached { @Sendable [storedWords] in
                        let encoder = JSONEncoder()
                        
                        if let data = try? encoder.encode(storedWords) {
                           if let url = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {
                              let path = url.appending(path: "words.json", directoryHint: .inferFromPath).path(percentEncoded: false)
                              
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
                           
                           if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
                              let documentsUrl = containerUrl.appending(path: "Documents", directoryHint: .isDirectory)
                              let documentsPath = documentsUrl.path(percentEncoded: false)
                              let url = documentsUrl.appending(path: ".words.json", directoryHint: .inferFromPath)
                              let path = url.path(percentEncoded: false)
                              
                              if !FileManager.default.fileExists(atPath: documentsPath) {
                                 try? FileManager.default.createDirectory(atPath: documentsPath, withIntermediateDirectories: false)
                              }
                              
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
                              
                              if let currentVersion = NSFileVersion.currentVersionOfItem(at: url), currentVersion.isConflict {
                                 try? NSFileVersion.removeOtherVersionsOfItem(at: url)
                                 
                                 if let conflictVersions = NSFileVersion.unresolvedConflictVersionsOfItem(at: url) {
                                    for fileVersion in conflictVersions {
                                       fileVersion.isResolved = true
                                    }
                                 }
                              }
                           }
                        }
                     }
                  }
               }
            }
         }
      }
      
      func agentDidTransition(_ agent: AgentView) {
         let isIdle = agent.idle
         
         if self.parent.idle != isIdle {
            self.parent.idle = isIdle
         }
      }
      
      func agentDidStop(_ agent: AgentView) {
         agent.change(path: self.parent.resource.new)
         
         self.parent.attributes.removeAll()
         self.parent.types = 0
         self.parent.choices.removeAll()
         self.parent.permissions.removeAll()
      }
      
      func agentDidChange(_ agent: AgentView) {
         self.parent.attributes.append(contentsOf: agent.attributes)
         
         withAnimation {
            self.parent.likability = nil
            self.parent.changing = false
         }
         
         self.lines.removeAll()
         
         if let uiView = self.uiView {
            uiView.reload(lines: [])
         }
      }
      
      func agentDidUpdate(_ agent: AgentView, background: [[(url: URL?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)]]?) {
         if let background, !agent.characterViews.contains(where: { view in
            if let character = Script.shared.characters.first(where: { $0.name == view.name }), character.prompt != nil {
               return true
            }
            
            return false
         }) || (self.parent.likability ?? 0.0) >= 0.5, let uiView = self.uiView {
            if !background.isEmpty {
               self.timestamp = Date()
            }
            
            Task {
               await uiView.reload(frames: background, particles: min(self.parent.words.count, 100))
            }
         }
      }
      
      func wallCanSelect(_ wall: WallView, at index: Int) -> Bool {
         return index < self.lines.count && !self.lines[index].attributes.isEmpty
      }
      
      func wallDidSelect(_ wall: WallView, at index: Int) {
         if index < self.lines.count {
            let line = self.lines[index]
            var ranges = [(start: Int, boundary: Int, end: Int, attributes: [String])]()
            var indexByStart = [Int: Int]()
            var attributeNamesByStart = [Int: Set<String>]()
            var letterSet = Set<Character>()
            var candidates = [(name: String, modifier: String?, attributes: [String])]()
            
            for attribute in line.attributes {
               if let rangeIndex = indexByStart[attribute.start] {
                  if attribute.end > ranges[rangeIndex].end {
                     ranges[rangeIndex].boundary = ranges[rangeIndex].end
                     ranges[rangeIndex].end = attribute.end
                  } else if attribute.end < ranges[rangeIndex].end && attribute.end > ranges[rangeIndex].boundary {
                     ranges[rangeIndex].boundary = attribute.end
                  }
               } else {
                  indexByStart[attribute.start] = ranges.count
                  ranges.append((start: attribute.start, boundary: attribute.start, end: attribute.end, attributes: []))
               }
            }
            
            for attribute in line.attributes {
               guard let rangeIndex = indexByStart[attribute.start], attribute.end == ranges[rangeIndex].end, let name = attribute.name else {
                  continue
               }
               
               if var set = attributeNamesByStart[attribute.start] {
                  if !set.contains(name) {
                     set.insert(name)
                     attributeNamesByStart[attribute.start] = set
                     ranges[rangeIndex].attributes.append(name)
                  }
               } else {
                  attributeNamesByStart[attribute.start] = [name]
                  ranges[rangeIndex].attributes.append(name)
               }
            }
            
            for range in ranges {
               let startIndex = line.text.index(line.text.startIndex, offsetBy: range.start)
               let boundaryIndex = line.text.index(line.text.startIndex, offsetBy: range.boundary)
               let endIndex = line.text.index(line.text.startIndex, offsetBy: range.end)
               let modifier: String?
               let name: String
               
               if range.boundary == range.start {
                  modifier = nil
                  name = String(line.text[startIndex..<endIndex])
               } else {
                  let s = String(line.text[startIndex..<boundaryIndex]).trimmingCharacters(in: .whitespaces)
                  
                  modifier = s
                  name = String(line.text[boundaryIndex..<endIndex])
                  
                  for i in 0..<s.count {
                     let character = s[s.index(s.startIndex, offsetBy: i)]
                     
                     if !letterSet.contains(character) && !character.isNewline && !character.isWhitespace {
                        letterSet.insert(character)
                     }
                  }
               }
               
               for i in 0..<name.count {
                  let character = name[name.index(name.startIndex, offsetBy: i)]
                  
                  if !letterSet.contains(character) && !character.isNewline && !character.isWhitespace {
                     letterSet.insert(character)
                  }
               }
               
               if candidates.contains(where: { $0.name == name && $0.modifier == modifier && $0.attributes == range.attributes }) {
                  candidates.append((name: name, modifier: modifier, attributes: range.attributes))
               }
            }
            
            if !candidates.isEmpty {
               let choice = candidates[Int.random(in: 0..<candidates.count)]
               
               self.parent.choices.removeAll()
               
               if let modifier = choice.modifier {
                  let word: Word
                  
                  if modifier.allSatisfy({ $0.isASCII }) && choice.name.allSatisfy({ $0.isASCII }) {
                     word = Word(name: modifier + String("\u{0020}\u{000A}") + choice.name, attributes: choice.attributes)
                  } else {
                     word = Word(name: modifier + "\n" + choice.name, attributes: choice.attributes)
                  }
                  
                  Task {
                     await self.talk(word: word, temperature: self.temperature, multiple: wall.subviews.compactMap({ $0 as? AgentView }).first.map({ agent in
                        let safeBounds = agent.bounds.inset(by: agent.safeAreaInsets)
                        
                        return safeBounds.width > safeBounds.height
                     }) ?? false, mute: self.parent.mute)
                  }
                  
                  self.parent.prompt = (choice.name, word, self.parent.prompt.2, letterSet, self.parent.prompt.4, self.parent.prompt.5, CACurrentMediaTime())
               } else {
                  let word = Word(name: choice.name, attributes: choice.attributes)
                  
                  Task {
                     await self.talk(word: word, temperature: self.temperature, multiple: wall.subviews.compactMap({ $0 as? AgentView }).first.map({ agent in
                        let safeBounds = agent.bounds.inset(by: agent.safeAreaInsets)
                        
                        return safeBounds.width > safeBounds.height
                     }) ?? false, mute: self.parent.mute)
                  }
                  
                  self.parent.prompt = (choice.name, word, self.parent.prompt.2, letterSet, self.parent.prompt.4, self.parent.prompt.5, CACurrentMediaTime())
               }
            }
         }
      }
      
      func wallDidLoad(_ wall: WallView, frames: [(image: CGImage, delay: Double)]) {
         Task.detached { [weak self] in
            var hasher = SHA256()
            
            for frame in frames {
               if let resizedImage = self?.resize(image: frame.image, maximum: 256, quality: .none) {
                  let meta = [UInt32(resizedImage.width), UInt32(resizedImage.height), UInt32(resizedImage.bitsPerPixel), UInt32(resizedImage.bytesPerRow)]
                  
                  meta.withUnsafeBytes { hasher.update(data: $0) }
                  
                  guard let dataProvider = resizedImage.dataProvider, let data = dataProvider.data, let bytes = CFDataGetBytePtr(data) else {
                     return
                  }
                  
                  hasher.update(data: Data(bytes: bytes, count: CFDataGetLength(data)))
               } else {
                  return
               }
            }
            
            if let encodedFrames = self?.encode(hasher.finalize()), let documentUrl = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {
               let payload = encodedFrames.suffix(16)
               var isNew = true
               
               if let urls = try? FileManager.default.contentsOfDirectory(at: documentUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
                  let regex = /(?i)([0-9A-HJKMNP-TV-Z]{26})(?=\.png$)/
                  
                  for url in urls {
                     if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), let isDirectory = values.isDirectory, !isDirectory, let name = values.name, let match = name.firstMatch(of: regex), payload == String(match.output.1).suffix(16) {
                        isNew = false
                        
                        break
                     }
                  }
               }
               
               if isNew {
                  let data = NSMutableData()
                  
                  guard let destination = CGImageDestinationCreateWithData(data, UTType.png.identifier as CFString, frames.count, nil) else {
                     return
                  }
                  
                  CGImageDestinationSetProperties(destination, [kCGImagePropertyPNGDictionary: [kCGImagePropertyAPNGLoopCount: 0]] as CFDictionary)
                  
                  for frame in frames {
                     CGImageDestinationAddImage(destination, frame.image, [kCGImagePropertyPNGDictionary: [kCGImagePropertyAPNGDelayTime: frame.delay]] as CFDictionary)
                  }
                  
                  if CGImageDestinationFinalize(destination) {
                     FileManager.default.createFile(atPath: documentUrl.appending(path: "\(encodedFrames).png", directoryHint: .inferFromPath).path(percentEncoded: false), contents: data as Data, attributes: nil)
                     
                     let image = UIImage(systemName: "arrow.down.doc", withConfiguration: UIImage.SymbolConfiguration(font: .systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .caption1).pointSize, weight: .bold)))!.applyingSymbolConfiguration(.preferringMonochrome())!
                     
                     await MainActor.run {
                        guard let uiView = self?.uiView else {
                           return
                        }
                        
                        for view in uiView.subviews {
                           if let agentView = view as? AgentView {
                              if let characterView = agentView.characterViews.first {
                                 agentView.notify(characterView: characterView, image: image, text: nil, duration: 3.0)
                              }
                              
                              break
                           }
                        }
                     }
                  }
               }
            }
         }
      }
      
      @MainActor
      private func talk(word: Word?, temperature: Double, multiple: Bool, mute: Bool) async -> Bool {
         var queue = Script.shared.characters
         
         if let first = queue.first {
            var logs = [(id: UUID?, from: String?, to: String?, group: Double, raw: String?, content: (text: String?, image: CGImage?), choices: [String]?)]()
            let time: Double
            var sequences = [(String, UUID?, String, Sequence, Double?, [(String, URL?)]?)]()
            
            if let word {
               let input = word.name.filter { !$0.isNewline }
               let attributes = word.attributes ?? []
               let generateRequired: Bool
               
               if multiple {
                  queue.removeFirst()
                  
                  if let last = self.parent.logs.last {
                     var isContinuous = false
                     
                     for log in self.parent.logs {
                        if log.group == last.group {
                           if log.from == nil, let choices = log.choices, choices.contains(where: { $0 == input }) {
                              isContinuous = true
                           }
                           
                           logs.append(log)
                        }
                     }
                     
                     if isContinuous {
                        generateRequired = true
                        time = last.group
                     } else {
                        logs.removeAll()
                        generateRequired = attributes.isEmpty || !first.sequences.contains(where: { $0.name == "Activate" })
                        time = CACurrentMediaTime()
                     }
                  } else {
                     generateRequired = attributes.isEmpty || !first.sequences.contains(where: { $0.name == "Activate" })
                     time = CACurrentMediaTime()
                  }
               } else {
                  queue.removeAll()
                  
                  if let last = self.parent.logs.last, self.parent.logs.contains(where: { x in
                     if x.from == nil && x.group == last.group, let choices = x.choices {
                        return choices.contains(where: { $0 == input })
                     }
                     
                     return false
                  }) {
                     generateRequired = true
                     time = last.group
                  } else {
                     generateRequired = attributes.isEmpty || !first.sequences.contains(where: { $0.name == "Activate" })
                     time = CACurrentMediaTime()
                  }
               }
               
               if var prompt = first.prompt, generateRequired {
                  withAnimation(.easeOut(duration: 0.5)) {
                     self.parent.loading = true
                  }
                  
                  if let memory = (await Task.detached {
                     if let data = self.load() {
                        return String(data: data, encoding: .utf8)
                     }
                     
                     return nil
                  }.value) {
                     prompt.append("\n\(memory)")
                  }
                  
                  var messages: [[String: Any]] = [["role": "system", "content": prompt]]
                  var i = logs.count - 1
                  
                  while i > 0 {
                     if let from = logs[i].from, from != first.name && logs[i - 1].from == first.name {
                        var parts = [[String: Any]]()
                        
                        if let text = logs[i].content.text {
                           parts.append(["type": "text", "text": text])
                        }
                        
                        if let image = logs[i].content.image {
                           if let dataURL = (await Task.detached {
                              var dataURL: String? = nil
                              
                              if let resizedImage = self.resize(image: image) {
                                 dataURL = self.convert(image: resizedImage)
                              }
                              
                              return dataURL
                           }.value) {
                              parts.append(["type": "image", "image": dataURL])
                           }
                        }
                        
                        if !parts.isEmpty {
                           if let raw = logs[i - 1].raw {
                              messages.insert(["role": "user", "content": parts], at: 1)
                              messages.insert(["role": "assistant", "content": raw], at: 1)
                           } else if let text = logs[i - 1].content.text {
                              messages.insert(["role": "user", "content": parts], at: 1)
                              messages.insert(["role": "assistant", "content": text], at: 1)
                           }
                        }
                        
                        i -= 2
                     } else {
                        i -= 1
                     }
                  }
                  
                  if messages.count == 1 {
                     var i = self.parent.logs.count - 1
                     
                     while i >= 0 {
                        if self.parent.logs[i].from == first.name && self.parent.logs[i].to == nil {
                           if i - 1 >= 0 && self.parent.logs[i].group == self.parent.logs[i - 1].group && self.parent.logs[i - 1].from == nil && self.parent.logs[i - 1].to == first.name {
                              var parts = [[String: Any]]()
                              
                              if let text = self.parent.logs[i - 1].content.text {
                                 parts.append(["type": "text", "text": text])
                              }
                              
                              if let image = self.parent.logs[i - 1].content.image {
                                 if let dataURL = (await Task.detached {
                                    var dataURL: String? = nil
                                    
                                    if let resizedImage = self.resize(image: image) {
                                       dataURL = self.convert(image: resizedImage)
                                    }
                                    
                                    return dataURL
                                 }.value) {
                                    parts.append(["type": "image", "image": dataURL])
                                 }
                              }
                              
                              if !parts.isEmpty {
                                 if let raw = self.parent.logs[i].raw {
                                    messages.insert(["role": "assistant", "content": raw], at: 1)
                                    messages.insert(["role": "user", "content": parts], at: 1)
                                 } else if let text = self.parent.logs[i].content.text {
                                    messages.insert(["role": "assistant", "content": text], at: 1)
                                    messages.insert(["role": "user", "content": parts], at: 1)
                                 }
                              }
                              
                              i -= 1
                           } else if let raw = self.parent.logs[i].raw {
                              messages.insert(["role": "assistant", "content": raw], at: 1)
                           } else if let text = self.parent.logs[i].content.text {
                              messages.insert(["role": "assistant", "content": text], at: 1)
                           }
                        }
                        
                        i -= 1
                     }
                     
                     messages.append(["role": "user", "content": [["type": "text", "text": input]]])
                  }
                  
                  if let (output, content, likability, terms, state, choices, memory, voice) = await self.generate(messages: messages, voice: mute ? nil : await self.sample(path: first.path, sequences: first.sequences), language: first.language, temperature: temperature) {
                     var sequence = Sequence(name: "Activate", state: nil)
                     let id = UUID()
                     var candidates = terms.reduce(into: [(target: String, words: [Word])]()) { output, value in
                        let term = value.filter { !$0.isEmpty }

                        guard let word = term.last else {
                           return
                        }

                        if term.count == 1 {
                           output.append((target: word, words: [Word(name: word, attributes: nil)]))
                        } else {
                           for index in 0..<term.count - 1 {
                              let parts = Array(term[index...])
                              let separator = parts.allSatisfy { $0.allSatisfy { $0.isASCII } } ? String("\u{0020}") : String()
                              
                              output.append((target: parts.joined(separator: separator), words: parts.enumerated().map { Word(name: $0.element, attributes: $0.offset < parts.count - 1 ? [] : nil) }))
                           }
                        }
                     }
                     
                     candidates.sort { $0.target.count > $1.target.count }
                     
                     var inlines: [(text: String, attributes: [String]?)] = content.isEmpty ? [] : [(content, nil)]
                     var words = [Word]()
                     
                     for candidate in candidates {
                        inlines = inlines.reduce(into: []) { output, inline in
                           guard inline.attributes == nil else {
                              output.append(inline)
                              
                              return
                           }

                           var text = inline.text

                           while let range = text.range(of: candidate.target, options: .caseInsensitive) {
                              if range.lowerBound != text.startIndex {
                                 output.append((text: String(text[..<range.lowerBound]), attributes: nil))
                              }
                              
                              var inline = String(text[range])

                              if candidate.words.count > 1, let lastWord = candidate.words.last?.name, let lastRange = inline.range(of: lastWord, options: [.caseInsensitive, .backwards]) {
                                 inline.insert("\n", at: lastRange.lowerBound)
                              }
                              
                              output.append((text: inline, attributes: []))
                              
                              for word in candidate.words {
                                 if !words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) && !self.parent.words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) {
                                    words.append(word)
                                 }
                              }

                              text = String(text[range.upperBound...])
                           }

                           if !text.isEmpty {
                              output.append((text: text, attributes: nil))
                           }
                        }
                     }
                     
                     sequence.append(.message(Message(id: id, inlines: inlines)))
                     
                     if let voice {
                        sequence.append(.audio(voice))
                     }
                     
                     sequence.append(.sequence(Sequence(name: "Emote", state: state ?? String())))
                     sequences.append((first.name, id, output, sequence, likability, choices))
                     
                     if !words.isEmpty {
                        self.parent.discoveries.append(contentsOf: words)
                     }
                     
                     if let memory {
                        await Task.detached {
                           if let data = memory.data(using: .utf8) {
                              self.save(data)
                           }
                        }.value
                     }
                     
                     while !queue.isEmpty {
                        let character = queue.removeFirst()
                        
                        if var prompt = character.prompt {
                           if let memory = (await Task.detached {
                              if let data = self.load() {
                                 return String(data: data, encoding: .utf8)
                              }
                              
                              return nil
                           }.value) {
                              prompt.append("\n\(memory)")
                           }
                           
                           var messages: [[String: Any]] = [["role": "system", "content": prompt], ["role": "user", "content": [["type": "text", "text": content]]]]
                           var i = logs.count - 1
                           
                           while i > 0 {
                              if logs[i].from == character.name {
                                 var parts = [[String: Any]]()
                                 
                                 if let text = logs[i - 1].content.text {
                                    parts.append(["type": "text", "text": text])
                                 }
                                 
                                 if let image = logs[i - 1].content.image {
                                    if let dataURL = (await Task.detached {
                                       var dataURL: String? = nil
                                       
                                       if let resizedImage = self.resize(image: image) {
                                          dataURL = self.convert(image: resizedImage)
                                       }
                                       
                                       return dataURL
                                    }.value) {
                                       parts.append(["type": "image", "image": dataURL])
                                    }
                                 }
                                 
                                 if !parts.isEmpty {
                                    if let raw = logs[i].raw {
                                       messages.insert(["role": "assistant", "content": raw], at: 1)
                                       messages.insert(["role": "user", "content": parts], at: 1)
                                    } else if let text = logs[i].content.text {
                                       messages.insert(["role": "assistant", "content": text], at: 1)
                                       messages.insert(["role": "user", "content": parts], at: 1)
                                    }
                                 }
                                 
                                 i -= 2
                              } else {
                                 i -= 1
                              }
                           }
                           
                           if let (output, content, _, terms, state, _, memory, voice) = await self.generate(messages: messages, voice: mute ? nil : self.sample(path: character.path, sequences: character.sequences), language: character.language, temperature: temperature) {
                              var sequence = Sequence(name: "Activate", state: nil)
                              let id = UUID()
                              var candidates = terms.reduce(into: [(target: String, words: [Word])]()) { output, value in
                                 let term = value.filter { !$0.isEmpty }

                                 guard let word = term.last else {
                                    return
                                 }

                                 if term.count == 1 {
                                    output.append((target: word, words: [Word(name: word, attributes: nil)]))
                                 } else {
                                    for index in 0..<term.count - 1 {
                                       let parts = Array(term[index...])
                                       let separator = parts.allSatisfy { $0.allSatisfy { $0.isASCII } } ? String("\u{0020}") : String()
                                       
                                       output.append((target: parts.joined(separator: separator), words: parts.enumerated().map { Word(name: $0.element, attributes: $0.offset < parts.count - 1 ? [] : nil) }))
                                    }
                                 }
                              }
                              
                              candidates.sort { $0.target.count > $1.target.count }
                              
                              var inlines: [(text: String, attributes: [String]?)] = content.isEmpty ? [] : [(content, nil)]
                              var words = [Word]()
                              
                              for candidate in candidates {
                                 inlines = inlines.reduce(into: []) { output, inline in
                                    guard inline.attributes == nil else {
                                       output.append(inline)
                                       
                                       return
                                    }

                                    var text = inline.text

                                    while let range = text.range(of: candidate.target, options: .caseInsensitive) {
                                       if range.lowerBound != text.startIndex {
                                          output.append((text: String(text[..<range.lowerBound]), attributes: nil))
                                       }
                                       
                                       var inline = String(text[range])

                                       if candidate.words.count > 1, let lastWord = candidate.words.last?.name, let lastRange = inline.range(of: lastWord, options: [.caseInsensitive, .backwards]) {
                                          inline.insert("\n", at: lastRange.lowerBound)
                                       }
                                       
                                       output.append((text: inline, attributes: []))
                                       
                                       for word in candidate.words {
                                          if !words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) && !self.parent.words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) {
                                             words.append(word)
                                          }
                                       }

                                       text = String(text[range.upperBound...])
                                    }

                                    if !text.isEmpty {
                                       output.append((text: text, attributes: nil))
                                    }
                                 }
                              }
                              
                              sequence.append(.message(Message(id: id, inlines: inlines)))
                              
                              if let voice {
                                 sequence.append(.audio(voice))
                              }
                              
                              sequence.append(.sequence(Sequence(name: "Emote", state: state ?? String())))
                              sequences.append((character.name, id, output, sequence, nil, nil))
                              
                              if !words.isEmpty {
                                 self.parent.discoveries.append(contentsOf: words)
                              }
                              
                              if let memory {
                                 await Task.detached {
                                    if let data = memory.data(using: .utf8) {
                                       self.save(data)
                                    }
                                 }.value
                              }
                           } else {
                              break
                           }
                        } else {
                           break
                        }
                     }
                  }
                  
                  withAnimation(.easeIn(duration: 0.5)) {
                     self.parent.loading = false
                  }
               }
               
               if sequences.isEmpty {
                  var i = 0
                  var term = String()
                  var modifier = String()
                  var oldSequences: [Sequence]? = nil
                  
                  while i < word.name.count {
                     let character = word.name[word.name.index(word.name.startIndex, offsetBy: i)]
                     
                     if character.isNewline {
                        modifier.append(contentsOf: term)
                        term.removeAll()
                     } else {
                        term.append(character)
                     }
                     
                     i += 1
                  }
                  
                  await Script.shared.run(name: first.name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                     if y.name == first.name {
                        for sequence in y.sequences {
                           if sequence.name == "Activate" {
                              x.append(sequence)
                           }
                        }
                     }
                  }), words: [Word(name: term, attributes: word.attributes)], temperature: temperature) { x in
                     if !x.isEmpty {
                        var y = x
                        
                        y.append(Sequence(name: String()))
                        oldSequences = y
                     }
                     
                     return []
                  }
                  
                  if let oldSequences {
                     var content = [String]()
                     var newSequences = [Sequence]()
                     
                     if !mute, let prompt = await self.sample(path: first.path, sequences: first.sequences), let language = first.language {
                        var generateRequired = false
                        
                        if modifier.isEmpty {
                           for sequence in oldSequences {
                              var tempSequence = Sequence(name: sequence.name)
                              
                              for (i, step) in sequence.enumerated() {
                                 if case .message(let message) = step {
                                    let s = message.reduce(into: String()) { content, inline in
                                       if inline.attributes == nil {
                                          content.append(inline.text)
                                       } else {
                                          content.append(inline.text.filter { !$0.isNewline })
                                       }
                                    }
                                    
                                    if i + 1 < sequence.count, case .sound = sequence[i + 1] {
                                       tempSequence.append(.message(message))
                                    } else {
                                       tempSequence.append(.synthesis(message, s))
                                       generateRequired = true
                                    }
                                    
                                    content.append(s)
                                 } else {
                                    tempSequence.append(step)
                                 }
                              }
                              
                              newSequences.append(tempSequence)
                           }
                        } else {
                           for sequence in oldSequences {
                              var tempSequence = Sequence(name: sequence.name)
                              
                              for (i, step) in sequence.enumerated() {
                                 if case .message(let message) = step {
                                    var m = Message()
                                    var s = String()
                                    
                                    for j in 0..<message.count {
                                       var isEqual = true
                                       
                                       if let a = message[j].attributes, message[j].text == term && a.count == attributes.count {
                                          for k in 0..<attributes.count {
                                             if attributes[k] != a[k] {
                                                isEqual = false
                                                
                                                break
                                             }
                                          }
                                       } else {
                                          isEqual = false
                                       }
                                       
                                       if isEqual {
                                          m.append((text: modifier + "\n" + term, attributes: message[j].attributes))
                                          s.append(modifier + term)
                                       } else {
                                          m.append((text: message[j].text, attributes: message[j].attributes))
                                          s.append(message[j].text)
                                       }
                                    }
                                    
                                    if i + 1 < sequence.count, case .sound = sequence[i + 1] {
                                       tempSequence.append(.message(m))
                                    } else {
                                       tempSequence.append(.synthesis(m, s))
                                       generateRequired = true
                                    }
                                    
                                    content.append(s)
                                 } else {
                                    tempSequence.append(step)
                                 }
                              }
                              
                              newSequences.append(tempSequence)
                           }
                        }
                        
                        if generateRequired {
                           withAnimation(.easeOut(duration: 0.5)) {
                              self.parent.loading = true
                           }
                           
                           for i in 0..<newSequences.count {
                              var tempSequence = Sequence(name: newSequences[i].name)
                              
                              for step in newSequences[i] {
                                 if case .synthesis(let message, let input) = step {
                                    tempSequence.append(.message(message))
                                    
                                    if let wave = await self.generate(prompt: prompt, input: input, language: language, temperature: temperature) {
                                       tempSequence.append(.audio(wave))
                                    }
                                 } else {
                                    tempSequence.append(step)
                                 }
                              }
                              
                              newSequences[i] = tempSequence
                           }
                           
                           withAnimation(.easeIn(duration: 0.5)) {
                              self.parent.loading = false
                           }
                        }
                     } else if modifier.isEmpty {
                        for sequence in oldSequences {
                           var tempSequence = Sequence(name: sequence.name)
                           
                           for step in sequence {
                              if case .message(let message) = step {
                                 tempSequence.append(.message(message))
                                 content.append(message.reduce(into: String(), { content, inline in
                                    if inline.attributes == nil {
                                       content.append(inline.text)
                                    } else {
                                       content.append(inline.text.filter { !$0.isNewline })
                                    }
                                 }))
                              } else {
                                 tempSequence.append(step)
                              }
                           }
                           
                           newSequences.append(tempSequence)
                        }
                     } else {
                        for sequence in oldSequences {
                           var tempSequence = Sequence(name: sequence.name)
                           
                           for step in sequence {
                              if case .message(let message) = step {
                                 var m = Message()
                                 var s = String()
                                 
                                 for i in 0..<message.count {
                                    var isEqual = true
                                    
                                    if let a = message[i].attributes, message[i].text == term && a.count == attributes.count {
                                       for j in 0..<attributes.count {
                                          if attributes[j] != a[j] {
                                             isEqual = false
                                             
                                             break
                                          }
                                       }
                                    } else {
                                       isEqual = false
                                    }
                                    
                                    if isEqual {
                                       m.append((text: modifier + "\n" + term, attributes: message[i].attributes))
                                       s.append(modifier + term)
                                    } else {
                                       m.append((text: message[i].text, attributes: message[i].attributes))
                                       s.append(message[i].text)
                                    }
                                 }
                                 
                                 tempSequence.append(.message(m))
                                 content.append(s)
                              } else {
                                 tempSequence.append(step)
                              }
                           }
                           
                           newSequences.append(tempSequence)
                        }
                     }
                     
                     self.parent.logs.append((id: nil, from: nil, to: first.name, group: time, raw: nil, content: (text: input, image: nil), choices: nil))
                     self.parent.logs.append((id: nil, from: first.name, to: nil, group: time, raw: nil, content: (text: content.joined(separator: "\n"), image: nil), choices: nil))
                     self.parent.choices.removeAll()
                     
                     for var sequence in newSequences {
                        sequence.append(.completion)
                        
                        Script.shared.queue.append((first.name, sequence))
                     }
                  } else {
                     return false
                  }
               } else {
                  for i in 0..<sequences.count {
                     await Script.shared.run(name: sequences[i].0, sequences: [sequences[i].3], words: []) { x in
                        var y = x
                        var content = [String]()
                        let choices: [String]?
                        
                        for sequence in x {
                           for step in sequence {
                              if case .message(let message) = step {
                                 content.append(message.reduce(into: String(), { content, inline in
                                    if inline.attributes == nil {
                                       content.append(inline.text)
                                    } else {
                                       content.append(inline.text.filter { !$0.isNewline })
                                    }
                                 }))
                              }
                           }
                        }
                        
                        y.append(Sequence(name: String()))
                        
                        if let c = sequences[i].5 {
                           choices = c.reduce(into: [String](), { x, y in
                              x.append(y.0)
                           })
                           self.parent.choices.removeAll()
                           self.parent.choices.append(contentsOf: c)
                        } else {
                           choices = nil
                        }
                        
                        if i > 0 {
                           self.parent.logs.append((id: sequences[i].1, from: sequences[i].0, to: sequences[0].0, group: time, raw: sequences[i].2, content: (text: content.joined(separator: "\n"), image: nil), choices: choices))
                        } else {
                           self.parent.logs.append((id: nil, from: nil, to: sequences[i].0, group: time, raw: nil, content: (text: input, image: nil), choices: choices))
                           self.parent.logs.append((id: sequences[i].1, from: sequences[i].0, to: nil, group: time, raw: sequences[i].2, content: (text: content.joined(separator: "\n"), image: nil), choices: choices))
                        }
                        
                        if let likability = sequences[i].4 {
                           withAnimation {
                              self.parent.likability = likability
                           }
                        }
                        
                        return y
                     }
                  }
               }
            } else if var prompt = first.prompt {
               if multiple {
                  queue.removeFirst()
                  
                  if let last = self.parent.logs.last {
                     for log in self.parent.logs {
                        if log.group == last.group {
                           logs.append(log)
                        }
                     }
                     
                     logs.removeAll()
                     time = CACurrentMediaTime()
                  } else {
                     time = CACurrentMediaTime()
                  }
               } else {
                  queue.removeAll()
                  time = CACurrentMediaTime()
               }
               
               withAnimation(.easeOut(duration: 0.5)) {
                  self.parent.loading = true
               }
               
               if let memory = (await Task.detached {
                  if let data = self.load() {
                     return String(data: data, encoding: .utf8)
                  }
                  
                  return nil
               }.value) {
                  prompt.append("\n\(memory)")
               }
               
               var messages: [[String: Any]] = [["role": "system", "content": prompt]]
               var i = logs.count - 1
               
               while i > 0 {
                  if let from = logs[i].from, from != first.name && logs[i - 1].from == first.name {
                     var parts = [[String: Any]]()
                     
                     if let text = logs[i].content.text {
                        parts.append(["type": "text", "text": text])
                     }
                     
                     if let image = logs[i].content.image {
                        if let dataURL = (await Task.detached {
                           var dataURL: String? = nil
                           
                           if let resizedImage = self.resize(image: image) {
                              dataURL = self.convert(image: resizedImage)
                           }
                           
                           return dataURL
                        }.value) {
                           parts.append(["type": "image", "image": dataURL])
                        }
                     }
                     
                     if !parts.isEmpty {
                        if let raw = logs[i - 1].raw {
                           messages.insert(["role": "user", "content": parts], at: 1)
                           messages.insert(["role": "assistant", "content": raw], at: 1)
                        } else if let text = logs[i - 1].content.text {
                           messages.insert(["role": "user", "content": parts], at: 1)
                           messages.insert(["role": "assistant", "content": text], at: 1)
                        }
                     }
                     
                     i -= 2
                  } else {
                     i -= 1
                  }
               }
               
               if messages.count == 1 {
                  var i = self.parent.logs.count - 1
                  
                  while i >= 0 {
                     if self.parent.logs[i].from == first.name && self.parent.logs[i].to == nil {
                        if i - 1 >= 0 && self.parent.logs[i].group == self.parent.logs[i - 1].group && self.parent.logs[i - 1].from == nil && self.parent.logs[i - 1].to == first.name {
                           var parts = [[String: Any]]()
                           
                           if let text = self.parent.logs[i - 1].content.text {
                              parts.append(["type": "text", "text": text])
                           }
                           
                           if let image = self.parent.logs[i - 1].content.image {
                              if let dataURL = (await Task.detached {
                                 var dataURL: String? = nil
                                 
                                 if let resizedImage = self.resize(image: image) {
                                    dataURL = self.convert(image: resizedImage)
                                 }
                                 
                                 return dataURL
                              }.value) {
                                 parts.append(["type": "image", "image": dataURL])
                              }
                           }
                           
                           if !parts.isEmpty {
                              if let raw = self.parent.logs[i].raw {
                                 messages.insert(["role": "assistant", "content": raw], at: 1)
                                 messages.insert(["role": "user", "content": parts], at: 1)
                              } else if let text = self.parent.logs[i].content.text {
                                 messages.insert(["role": "assistant", "content": text], at: 1)
                                 messages.insert(["role": "user", "content": parts], at: 1)
                              }
                           }
                           
                           i -= 1
                        } else if let raw = self.parent.logs[i].raw {
                           messages.insert(["role": "assistant", "content": raw], at: 1)
                        } else if let text = self.parent.logs[i].content.text {
                           messages.insert(["role": "assistant", "content": text], at: 1)
                        }
                     }
                     
                     i -= 1
                  }
               }
               
               if let (output, content, likability, terms, state, choices, memory, voice) = await self.generate(messages: messages, voice: mute ? nil : await self.sample(path: first.path, sequences: first.sequences), language: first.language, temperature: temperature) {
                  var sequence = Sequence(name: "Activate", state: nil)
                  let id = UUID()
                  var candidates = terms.reduce(into: [(target: String, words: [Word])]()) { output, value in
                     let term = value.filter { !$0.isEmpty }

                     guard let word = term.last else {
                        return
                     }

                     if term.count == 1 {
                        output.append((target: word, words: [Word(name: word, attributes: nil)]))
                     } else {
                        for index in 0..<term.count - 1 {
                           let parts = Array(term[index...])
                           let separator = parts.allSatisfy { $0.allSatisfy { $0.isASCII } } ? String("\u{0020}") : String()
                           
                           output.append((target: parts.joined(separator: separator), words: parts.enumerated().map { Word(name: $0.element, attributes: $0.offset < parts.count - 1 ? [] : nil) }))
                        }
                     }
                  }
                  
                  candidates.sort { $0.target.count > $1.target.count }
                  
                  var inlines: [(text: String, attributes: [String]?)] = content.isEmpty ? [] : [(content, nil)]
                  var words = [Word]()
                  
                  for candidate in candidates {
                     inlines = inlines.reduce(into: []) { output, inline in
                        guard inline.attributes == nil else {
                           output.append(inline)
                           
                           return
                        }

                        var text = inline.text

                        while let range = text.range(of: candidate.target, options: .caseInsensitive) {
                           if range.lowerBound != text.startIndex {
                              output.append((text: String(text[..<range.lowerBound]), attributes: nil))
                           }
                           
                           var inline = String(text[range])

                           if candidate.words.count > 1, let lastWord = candidate.words.last?.name, let lastRange = inline.range(of: lastWord, options: [.caseInsensitive, .backwards]) {
                              inline.insert("\n", at: lastRange.lowerBound)
                           }
                           
                           output.append((text: inline, attributes: []))
                           
                           for word in candidate.words {
                              if !words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) && !self.parent.words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) {
                                 words.append(word)
                              }
                           }

                           text = String(text[range.upperBound...])
                        }

                        if !text.isEmpty {
                           output.append((text: text, attributes: nil))
                        }
                     }
                  }
                  
                  sequence.append(.message(Message(id: id, inlines: inlines)))
                  
                  if let voice {
                     sequence.append(.audio(voice))
                  }
                  
                  sequence.append(.sequence(Sequence(name: "Emote", state: state ?? String())))
                  sequences.append((first.name, id, output, sequence, likability, choices))
                  
                  if !words.isEmpty {
                     self.parent.discoveries.append(contentsOf: words)
                  }
                  
                  if let memory {
                     await Task.detached {
                        if let data = memory.data(using: .utf8) {
                           self.save(data)
                        }
                     }.value
                  }
                  
                  while !queue.isEmpty {
                     let character = queue.removeFirst()
                     
                     if var prompt = character.prompt {
                        if let memory = (await Task.detached {
                           if let data = self.load() {
                              return String(data: data, encoding: .utf8)
                           }
                           
                           return nil
                        }.value) {
                           prompt.append("\n\(memory)")
                        }
                        
                        var messages: [[String: Any]] = [["role": "system", "content": prompt], ["role": "user", "content": [["type": "text", "text": content]]]]
                        var i = logs.count - 1
                        
                        while i > 0 {
                           if logs[i].from == character.name {
                              var parts = [[String: Any]]()
                              
                              if let text = logs[i - 1].content.text {
                                 parts.append(["type": "text", "text": text])
                              }
                              
                              if let image = logs[i - 1].content.image {
                                 if let dataURL = (await Task.detached {
                                    var dataURL: String? = nil
                                    
                                    if let resizedImage = self.resize(image: image) {
                                       dataURL = self.convert(image: resizedImage)
                                    }
                                    
                                    return dataURL
                                 }.value) {
                                    parts.append(["type": "image", "image": dataURL])
                                 }
                              }
                              
                              if !parts.isEmpty {
                                 if let raw = logs[i].raw {
                                    messages.insert(["role": "assistant", "content": raw], at: 1)
                                    messages.insert(["role": "user", "content": parts], at: 1)
                                 } else if let text = logs[i].content.text {
                                    messages.insert(["role": "assistant", "content": text], at: 1)
                                    messages.insert(["role": "user", "content": parts], at: 1)
                                 }
                              }
                              
                              i -= 2
                           } else {
                              i -= 1
                           }
                        }
                        
                        if let (output, content, _, terms, state, _, memory, voice) = await self.generate(messages: messages, voice: mute ? nil : self.sample(path: character.path, sequences: character.sequences), language: character.language, temperature: temperature) {
                           var sequence = Sequence(name: "Activate", state: nil)
                           let id = UUID()
                           var candidates = terms.reduce(into: [(target: String, words: [Word])]()) { output, value in
                              let term = value.filter { !$0.isEmpty }

                              guard let word = term.last else {
                                 return
                              }

                              if term.count == 1 {
                                 output.append((target: word, words: [Word(name: word, attributes: nil)]))
                              } else {
                                 for index in 0..<term.count - 1 {
                                    let parts = Array(term[index...])
                                    let separator = parts.allSatisfy { $0.allSatisfy { $0.isASCII } } ? String("\u{0020}") : String()
                                    
                                    output.append((target: parts.joined(separator: separator), words: parts.enumerated().map { Word(name: $0.element, attributes: $0.offset < parts.count - 1 ? [] : nil) }))
                                 }
                              }
                           }
                           
                           candidates.sort { $0.target.count > $1.target.count }
                           
                           var inlines: [(text: String, attributes: [String]?)] = content.isEmpty ? [] : [(content, nil)]
                           var words = [Word]()
                           
                           for candidate in candidates {
                              inlines = inlines.reduce(into: []) { output, inline in
                                 guard inline.attributes == nil else {
                                    output.append(inline)
                                    
                                    return
                                 }

                                 var text = inline.text

                                 while let range = text.range(of: candidate.target, options: .caseInsensitive) {
                                    if range.lowerBound != text.startIndex {
                                       output.append((text: String(text[..<range.lowerBound]), attributes: nil))
                                    }
                                    
                                    var inline = String(text[range])

                                    if candidate.words.count > 1, let lastWord = candidate.words.last?.name, let lastRange = inline.range(of: lastWord, options: [.caseInsensitive, .backwards]) {
                                       inline.insert("\n", at: lastRange.lowerBound)
                                    }
                                    
                                    output.append((text: inline, attributes: []))
                                    
                                    for word in candidate.words {
                                       if !words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) && !self.parent.words.contains(where: { $0.name.compare(word.name, options: [.caseInsensitive]) == .orderedSame }) {
                                          words.append(word)
                                       }
                                    }

                                    text = String(text[range.upperBound...])
                                 }

                                 if !text.isEmpty {
                                    output.append((text: text, attributes: nil))
                                 }
                              }
                           }
                           
                           sequence.append(.message(Message(id: id, inlines: inlines)))
                           
                           if let voice {
                              sequence.append(.audio(voice))
                           }
                           
                           sequence.append(.sequence(Sequence(name: "Emote", state: state ?? String())))
                           sequences.append((character.name, id, output, sequence, nil, nil))
                           
                           if !words.isEmpty {
                              self.parent.discoveries.append(contentsOf: words)
                           }
                           
                           if let memory {
                              await Task.detached {
                                 if let data = memory.data(using: .utf8) {
                                    self.save(data)
                                 }
                              }.value
                           }
                        } else {
                           sequences.removeAll()
                           queue.removeAll()
                           
                           break
                        }
                     } else {
                        sequences.removeAll()
                        queue.removeAll()
                        
                        break
                     }
                  }
               } else {
                  queue.removeAll()
               }
               
               withAnimation(.easeIn(duration: 0.5)) {
                  self.parent.loading = false
               }
               
               for i in 0..<sequences.count {
                  await Script.shared.run(name: sequences[i].0, sequences: [sequences[i].3], words: []) { x in
                     var y = x
                     var content = [String]()
                     let choices: [String]?
                     
                     for sequence in x {
                        for step in sequence {
                           if case .message(let message) = step {
                              content.append(message.reduce(into: String(), { content, inline in
                                 if inline.attributes == nil {
                                    content.append(inline.text)
                                 } else {
                                    content.append(inline.text.filter { !$0.isNewline })
                                 }
                              }))
                           }
                        }
                     }
                     
                     y.append(Sequence(name: String()))
                     
                     if let c = sequences[i].5 {
                        choices = c.reduce(into: [String](), { x, y in
                           x.append(y.0)
                        })
                        self.parent.choices.removeAll()
                        self.parent.choices.append(contentsOf: c)
                     } else {
                        choices = nil
                     }
                     
                     if i > 0 {
                        self.parent.logs.append((id: sequences[i].1, from: sequences[i].0, to: sequences[0].0, group: time, raw: sequences[i].2, content: (text: content.joined(separator: "\n"), image: nil), choices: choices))
                     } else {
                        self.parent.logs.append((id: sequences[i].1, from: sequences[i].0, to: nil, group: time, raw: sequences[i].2, content: (text: content.joined(separator: "\n"), image: nil), choices: choices))
                     }
                     
                     if let likability = sequences[i].4 {
                        withAnimation {
                           self.parent.likability = likability
                        }
                     }
                     
                     return y
                  }
               }
            } else {
               return false
            }
            
            while self.parent.logs.count > 10 {
               let group = self.parent.logs[0].group
               
               for i in stride(from: self.parent.logs.count - 1, through: 0, by: -1) {
                  if self.parent.logs[i].group == group {
                     self.parent.logs.remove(at: i)
                  }
               }
            }
         }
         
         return true
      }
      
      private nonisolated func load(from filename: String = "MEMORY.md") -> Data? {
         if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
            let path = containerUrl.appendingPathComponent("Documents/\(filename)").path(percentEncoded: false)
            
            if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
               defer {
                  try? file.close()
               }
               
               return try? file.readToEnd()
            }
         }
         
         return nil
      }
      
      private nonisolated func save(_ data: Data, to filename: String = "MEMORY.md") {
         if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
            let path = containerUrl.appendingPathComponent("Documents/\(filename)").path(percentEncoded: false)
            
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
      }
      
      private func sample(path: String, sequences: [Sequence]) async -> Data? {
         return await Task.detached {
            var sequenceQueue = sequences
            
            while !sequenceQueue.isEmpty {
               let sequence = sequenceQueue.removeFirst()
               var index: Int? = nil
               
               for (i, step) in sequence.enumerated() {
                  if case .sequence(let s) = step {
                     sequenceQueue.append(s)
                  } else if case .message = step {
                     index = i
                  } else if case .sound(let sound) = step, i - 1 == index, let soundPath = sound.path {
                     let path = URL(filePath: path).deletingLastPathComponent().appending(path: soundPath, directoryHint: .inferFromPath).path(percentEncoded: false)
                     
                     if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                        defer {
                           try? file.close()
                        }
                        
                        if let data = try? file.readToEnd(), data.count > 44, let riff = String(data: data[0..<4], encoding: .ascii), riff == "RIFF", let wave = String(data: data[8..<12], encoding: .ascii), wave == "WAVE" && String(data: data[12..<16], encoding: .ascii) == "fmt " {
                           let sampleRate = data.subdata(in: 24..<28).withUnsafeBytes { $0.load(as: UInt32.self) }
                           let channels = data.subdata(in: 22..<24).withUnsafeBytes { $0.load(as: UInt16.self) }
                           let bitsPerSample = data.subdata(in: 34..<36).withUnsafeBytes { $0.load(as: UInt16.self) }
                           var dataChunkOffset = 36
                           
                           while dataChunkOffset + 8 < data.count {
                              let chunkID = String(data: data[dataChunkOffset..<dataChunkOffset + 4], encoding: .ascii)
                              let chunkSize = data.subdata(in: dataChunkOffset + 4..<dataChunkOffset + 8).withUnsafeBytes { $0.load(as: UInt32.self) }
                              
                              if chunkID == "data" {
                                 let duration = Double(Int(chunkSize) / Int(bitsPerSample / 8 * channels)) / Double(sampleRate)
                                 
                                 if duration > 3.0 && duration <= 10.0 {
                                    return data
                                 }
                                 
                                 break
                              }
                              
                              dataChunkOffset += 8 + Int(chunkSize)
                           }
                        }
                     }
                  }
               }
            }
            
            return nil
         }.value
      }
      
      private func generate(messages: [[String: Any]], voice: Data?, language: String?, temperature: Double) async -> (String, String, Double?, [[String]], String?, [(String, URL?)], String?, Data?)? {
         if let data = try? JSONSerialization.data(withJSONObject: ["messages": messages, "temperature": round(temperature * 10.0) / 10.0]) {
            var request = URLRequest(url: URL(string: "https://milchchan.com/api/generate")!)
            
            request.httpMethod = "POST"
            request.setValue("application/json", forHTTPHeaderField: "Content-Type")
            request.httpBody = data
            request.timeoutInterval = 60.0
            
            if let (data, response) = try? await URLSession.shared.data(for: request), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "application/json", let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [String: Any], let content = jsonRoot["content"] as? String {
               var likability: Double? = nil
               var terms = [[String]]()
               var state: String? = nil
               var choices = [(String, URL?)]()
               var memory: String? = nil
               var wave: Data? = nil
               
               if let value = jsonRoot["likability"] as? Double {
                  likability = value
               }
               
               if let value = jsonRoot["terms"] as? [Any] {
                  for item in value {
                     if let s = item as? String {
                        terms.append([s])
                     } else if let a = item as? [String] {
                        terms.append(a)
                     }
                  }
               }
               
               if let states = jsonRoot["states"] as? [String: Any] {
                  var max = 0.0
                  
                  for (key, object) in states {
                     if let value = object as? Double, value > max {
                        state = key
                        max = value
                     }
                  }
               }
               
               if let objects = jsonRoot["choices"] as? [Any] {
                  for object in objects {
                     if let value = object as? String {
                        choices.append((value, nil))
                     } else if let dictionary = object as? [String: Any?] {
                        if let text = dictionary["text"] as? String {
                           if let value = dictionary["url"] as? String {
                              if value.lowercased().hasPrefix("https://"), let url = URL(string: value) {
                                 choices.append((text, url))
                              }
                           } else {
                              choices.append((text, nil))
                           }
                        }
                     }
                  }
               }
               
               if let value = jsonRoot["memory"] as? String {
                  memory = value
               }
               
               if let voice, let language {
                  wave = await self.generate(prompt: voice, input: content, language: language, temperature: temperature)
               }
               
               if let data = try? JSONSerialization.data(withJSONObject: jsonRoot, options: .prettyPrinted), let output = String(data: data, encoding: .utf8) {
                  return (output, content, likability, terms, state, choices, memory, wave)
               }
            }
         }
         
         return nil
      }
      
      private func generate(prompt: Data, input: String, language: String, temperature: Double) async -> Data? {
         if let data = try? JSONSerialization.data(withJSONObject: ["input": input, "language": language, "temperature": round(temperature * 10.0) / 10.0]) {
            let request = await Task.detached {
               var request = URLRequest(url: URL(string: "https://milchchan.com/api/generate")!)
               let boundary = UUID().uuidString
               var body = Data()
               
               body.append("--\(boundary)\r\n".data(using: .utf8)!)
               body.append("Content-Disposition: form-data; name=\"file\"; filename=\"prompt.wav\"\r\n".data(using: .utf8)!)
               body.append("Content-Type: audio/wav\r\n".data(using: .utf8)!)
               body.append("Content-Transfer-Encoding: binary\r\n\r\n".data(using: .utf8)!)
               body.append(prompt)
               body.append("\r\n".data(using: .utf8)!)
               body.append("--\(boundary)\r\n".data(using: .utf8)!)
               body.append("Content-Disposition: form-data; name=\"data\"\r\n".data(using: .utf8)!)
               body.append("Content-Type: application/json\r\n\r\n".data(using: .utf8)!)
               body.append(data)
               body.append("\r\n".data(using: .utf8)!)
               body.append("--\(boundary)--\r\n".data(using: .utf8)!)
               
               request.httpMethod = "POST"
               request.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
               request.httpBody = body
               request.timeoutInterval = 60.0
               
               return request
            }.value
            
            if let (data, response) = try? await URLSession.shared.data(for: request), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "audio/wav" {
               return data
            }
         }
         
         return nil
      }
      
      private nonisolated func resize(image: CGImage, maximum: Double = 768, quality: CGInterpolationQuality = .high) -> CGImage? {
         let imageWidth = Double(image.width)
         let imageHeight = Double(image.height)
         let width: Double
         let height: Double
         
         if imageWidth < imageHeight {
            if imageHeight > maximum {
               width = floor(maximum / imageHeight * imageWidth)
               height = maximum
            } else {
               width = imageWidth
               height = imageHeight
            }
         } else if imageWidth > maximum {
            width = maximum
            height = floor(maximum / imageWidth * imageHeight)
         } else {
            width = imageWidth
            height = imageHeight
         }
         
         let size = CGSize(width: width, height: height)
         let rendererFormat = UIGraphicsImageRendererFormat()
         
         rendererFormat.opaque = false
         rendererFormat.scale = 1.0
         rendererFormat.preferredRange = .standard
         
         let renderer = UIGraphicsImageRenderer(size: size, format: rendererFormat)
         let resizedImage = renderer.image { rendererContext in
            let context = rendererContext.cgContext
            
            context.interpolationQuality = quality
            context.setAllowsAntialiasing(true)
            context.clear(CGRect(x: 0.0, y: 0.0, width: width, height: height))
            context.translateBy(x: 0.0, y: height)
            context.scaleBy(x: 1.0, y: -1.0)
            context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: width, height: height))
         }.cgImage
         
         return resizedImage
      }
      
      private nonisolated func convert(image: CGImage) -> String? {
         let mutableData = NSMutableData()
         
         guard let destination = CGImageDestinationCreateWithData(mutableData, UTType.jpeg.identifier as CFString, 1, nil) else {
            return nil
         }
         
         CGImageDestinationAddImage(destination, image, [kCGImageDestinationLossyCompressionQuality: 0.75] as CFDictionary)
         
         guard CGImageDestinationFinalize(destination) else {
            return nil
         }
         
         return "data:image/jpeg;base64,\(mutableData.base64EncodedString(options: []))"
      }
      
      private nonisolated func encode(_ digest: SHA256.Digest) -> String {
         let alphabet: [Character] = Array("0123456789ABCDEFGHJKMNPQRSTVWXYZ")
         var time = UInt64(Date().timeIntervalSince1970 * 1000)
         var timeChars = [Character](repeating: "0", count: 10)
         var digestChars = [Character]()
         var bitBuffer = 0
         var bitCount = 0
         
         for i in stride(from: 9, through: 0, by: -1) {
            timeChars[i] = alphabet[Int(time & 0x1F)]
            time >>= 5
         }
         
         digestChars.reserveCapacity(16)
         
         for byte in digest.prefix(10) {
            bitBuffer = bitBuffer << 8 | Int(byte)
            bitCount += 8
            
            while bitCount >= 5 && digestChars.count < 16 {
               bitCount -= 5
               digestChars.append(alphabet[bitBuffer >> bitCount & 0x1F])
            }
         }
         
         return String(timeChars + digestChars)
      }
   }
}

struct Prompt: UIViewRepresentable {
   let active: Bool
   let input: (String?, Word?, Bool, Set<Character>?, [(String, URL?)], Int, Double)?
   let accent: UIColor
   let font: UIFont
   
   func makeUIView(context: Context) -> PromptView {
      return PromptView(frame: .zero)
   }
   
   func updateUIView(_ uiView: PromptView, context: Context) {
      uiView.running = self.active
      uiView.accent = self.accent
      uiView.font = self.font
      
      if let input = self.input, let text = input.0 {
         if context.coordinator.timestamp != input.6 {
            if context.coordinator.text == text && !input.2 && input.5 == 0 && uiView.isScrambled {
               uiView.isScrambled = false
            } else  {
               if input.5 > 0 {
                  uiView.isScrambled = false
                  uiView.scrambleLetters = nil
               } else {
                  uiView.isScrambled = input.2
                  uiView.scrambleLetters = input.3
               }
               
               uiView.reload(text: text.filter { !$0.isNewline })
            }
            
            context.coordinator.text = text
            context.coordinator.timestamp = input.6
         }
      } else {
         uiView.reload(text: nil)
         
         context.coordinator.text = nil
      }
   }
   
   func makeCoordinator() -> Coordinator {
      return Coordinator()
   }
   
   class Coordinator: NSObject {
      var text: String? = nil
      var timestamp = 0.0
   }
}

struct Peek: UIViewControllerRepresentable {
   @Binding private var peekable: Bool
   private var ready: Bool
   private var pause: Bool
   private let onChange: @MainActor (CGImage) -> Void
   
   init(peekable: Binding<Bool>, ready: Bool, pause: Bool, onChange: @escaping @MainActor (CGImage) -> Void) {
      self._peekable = peekable
      self.ready = ready
      self.pause = pause
      self.onChange = onChange
   }
   
   func makeUIViewController(context: Context) -> PeekViewController {
      let viewController = PeekViewController()
      
      viewController.delegate = context.coordinator
      
      return viewController
   }
   
   func updateUIViewController(_ uiViewController: PeekViewController, context: Context) {
      if self.ready != uiViewController.isReady {
         uiViewController.isReady = self.ready
      }
      
      if self.pause != uiViewController.isPaused {
         uiViewController.isPaused = self.pause
      }
      
      context.coordinator.peekable = self.$peekable
      context.coordinator.onChange = self.onChange
   }
   
   func makeCoordinator() -> Coordinator {
      return Coordinator(peekable: self.$peekable, onChange: self.onChange)
   }
   
   protocol PeekDelegate: AnyObject {
      @MainActor
      func peekDidUpdate(_ peek: PeekViewController)
      @MainActor
      func peekDidFail(_ peek: PeekViewController)
   }
   
   class Coordinator: NSObject, PeekDelegate {
      var peekable: Binding<Bool>
      var onChange: @MainActor (_ image: CGImage) -> Void
      
      init(peekable: Binding<Bool>, onChange: @escaping @MainActor (_ image: CGImage) -> Void) {
         self.peekable = peekable
         self.onChange = onChange
      }
      
      @MainActor
      func peekDidUpdate(_ peek: Peek.PeekViewController) {
         guard let peekedImage = peek.peekedImage else {
            return
         }
         
         peek.flash()
         self.onChange(peekedImage)
      }
      
      @MainActor
      func peekDidFail(_ peek: Peek.PeekViewController) {
         self.peekable.wrappedValue = peek.isPeekable
      }
   }
   
   class PeekViewController: UIViewController, AVCaptureVideoDataOutputSampleBufferDelegate {
      var delegate: PeekDelegate? = nil
      var isPeekable = true
      var isPaused = false {
         didSet {
            if self.isPaused != oldValue {
               self.frameState.withLock { state in
                  state.generation &+= 1
               }
            }
         }
      }
      var isReady = false {
         didSet {
            if self.isReady != oldValue {
               self.frameState.withLock { state in
                  state.generation &+= 1
               }
            }
         }
      }
      var peekedImage: CGImage? = nil
      private let sessionQueue = DispatchQueue(label: String(describing: Peek.PeekViewController.self))
      private let captureSession = Mutex(AVCaptureSession())
      private let frameState = Mutex((isProcessing: false, generation: UInt64(0)))
      private var captureVideoPreviewLayer: AVCaptureVideoPreviewLayer? = nil
      private var isCaptureSessionConfigured = false
      private var isConfiguringCaptureSession = false
      private var isViewVisible = false
      private var elapsedTime = CACurrentMediaTime()
      private let threshold = 8
      private var peekedImageHash: UInt64 = 0

      override func viewDidLoad() {
         super.viewDidLoad()
         
         switch AVCaptureDevice.authorizationStatus(for: .video) {
         case .authorized:
            self.prepareCaptureSession()
            
         case .notDetermined:
            AVCaptureDevice.requestAccess(for: .video, completionHandler: { [weak self] granted in
               Task { @MainActor in
                  guard let self else {
                     return
                  }
                  
                  if granted {
                     self.prepareCaptureSession()
                  } else {
                     self.isPeekable = false
                     self.delegate?.peekDidFail(self)
                  }
               }
            })
            
         default:
            self.isPeekable = false
            self.delegate?.peekDidFail(self)
         }
      }

      private func prepareCaptureSession() {
         guard self.isPeekable && !self.isCaptureSessionConfigured && !self.isConfiguringCaptureSession else {
            return
         }

         self.isConfiguringCaptureSession = true

         if self.captureVideoPreviewLayer == nil {
            self.captureVideoPreviewLayer = self.captureSession.withLock { session in
               AVCaptureVideoPreviewLayer(session: session)
            }
            self.captureVideoPreviewLayer!.videoGravity = AVLayerVideoGravity.resizeAspectFill
            self.captureVideoPreviewLayer!.frame = self.view.bounds
            
            self.view.layer.addSublayer(self.captureVideoPreviewLayer!)
         }

         self.sessionQueue.async { [weak self] in
            guard let self else {
               return
            }

            let configured = self.configureCaptureSession()

            Task { @MainActor [weak self] in
               guard let self else {
                  return
               }

               self.isConfiguringCaptureSession = false

               if configured {
                  self.isCaptureSessionConfigured = true

                  if self.isViewVisible {
                     self.startCaptureSession()
                  }
               } else {
                  self.isCaptureSessionConfigured = false
                  self.isPeekable = false
                  self.delegate?.peekDidFail(self)
               }
            }
         }
      }

      private func startCaptureSession() {
         guard self.isPeekable && self.isCaptureSessionConfigured && self.isViewVisible else {
            return
         }

         self.sessionQueue.async { [weak self] in
            guard let self else {
               return
            }
            
            self.captureSession.withLock { session in
               if !session.isRunning {
                  session.startRunning()
               }
            }
            
            Task { @MainActor [weak self] in
               guard let self, let captureVideoPreviewLayer = self.captureVideoPreviewLayer else {
                  return
               }
               
               let angle: CGFloat
               
               switch self.view.window?.windowScene?.effectiveGeometry.interfaceOrientation ?? .portrait {
               case .portraitUpsideDown:
                  angle = 270
               case .landscapeLeft:
                  angle = 180
               case .landscapeRight:
                  angle = 0
               default:
                  angle = 90
               }
               
               if let connection = captureVideoPreviewLayer.connection, connection.isVideoRotationAngleSupported(angle) {
                  connection.videoRotationAngle = angle
               }
            }
         }
      }

      private func stopCaptureSession() {
         guard self.isCaptureSessionConfigured else {
            return
         }

         self.sessionQueue.async { [weak self] in
            self?.captureSession.withLock { session in
               if session.isRunning {
                  session.stopRunning()
               }
            }
         }
      }

      nonisolated private func configureCaptureSession() -> Bool {
         return self.captureSession.withLock { session in
            guard let captureDevice = AVCaptureDevice.default(for: .video), let input = try? AVCaptureDeviceInput(device: captureDevice) else {
               return false
            }

            let output = AVCaptureVideoDataOutput()

            output.videoSettings = [kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32BGRA]
            output.setSampleBufferDelegate(self, queue: self.sessionQueue)
            output.alwaysDiscardsLateVideoFrames = true

            guard session.canAddInput(input) else {
               return false
            }

            session.beginConfiguration()
            
            defer {
               session.commitConfiguration()
            }

            session.addInput(input)

            guard session.canAddOutput(output) else {
               session.removeInput(input)

               return false
            }

            session.addOutput(output)

            if let connection = output.connection(with: .video), connection.isVideoStabilizationSupported {
               connection.preferredVideoStabilizationMode = .auto
            }

            if session.canSetSessionPreset(.photo) {
               session.sessionPreset = .photo
            }

            return true
         }
      }
      
      override func viewDidLayoutSubviews() {
         super.viewDidLayoutSubviews()

         if let captureVideoPreviewLayer = self.captureVideoPreviewLayer {
            let angle: CGFloat
            let sizeChanged = captureVideoPreviewLayer.bounds.size != self.view.bounds.size
            
            captureVideoPreviewLayer.frame = self.view.bounds
            
            switch self.view.window?.windowScene?.effectiveGeometry.interfaceOrientation ?? .portrait {
            case .portraitUpsideDown:
               angle = 270
            case .landscapeLeft:
               angle = 180
            case .landscapeRight:
               angle = 0
            default:
               angle = 90
            }
            
            if let connection = captureVideoPreviewLayer.connection, connection.isVideoRotationAngleSupported(angle) {
               connection.videoRotationAngle = angle
            }

            if sizeChanged {
               self.frameState.withLock { state in
                  state.generation &+= 1
               }
            }
         }
      }
      
      override func viewWillAppear(_ animated: Bool) {
         super.viewWillAppear(animated)
         
         self.frameState.withLock { state in
            state.generation &+= 1
         }
         self.isViewVisible = true
         self.startCaptureSession()
      }
      
      override func viewWillDisappear(_ animated: Bool) {
         self.isViewVisible = false
         self.frameState.withLock { state in
            state.generation &+= 1
         }
         self.stopCaptureSession()
         
         super.viewWillDisappear(animated)
      }
      
      nonisolated func captureOutput(_ output: AVCaptureOutput, didOutput sampleBuffer: CMSampleBuffer, from connection: AVCaptureConnection) {
         guard let pixelBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) else {
            return
         }

         let generation = self.frameState.withLock { state -> UInt64? in
            guard !state.isProcessing else {
               return nil
            }

            state.isProcessing = true

            return state.generation
         }

         guard let generation else {
            return
         }

         let sourceImage = CIImage(cvImageBuffer: pixelBuffer)

         Task { @MainActor [weak self] in
            guard let self else {
               return
            }

            defer {
               self.frameState.withLock { state in
                  state.isProcessing = false
               }
            }

            let isCurrent = self.frameState.withLock { state in
               state.generation == generation
            }

            guard isCurrent else {
               return
            }

            let orientation: CGImagePropertyOrientation

            switch self.view.window?.windowScene?.effectiveGeometry.interfaceOrientation ?? .portrait {
            case .portraitUpsideDown:
               orientation = .left
            case .landscapeLeft:
               orientation = .down
            case .landscapeRight:
               orientation = .up
            default:
               orientation = .right
            }

            let image = sourceImage.oriented(orientation)
            let currentMediaTime = CACurrentMediaTime()

            if self.isViewVisible && !self.isPaused && self.isReady && currentMediaTime - self.elapsedTime >= 1.0 {
               let viewSize = self.view.bounds.size

               guard viewSize.width > 0.0 && viewSize.height > 0.0 else {
                  return
               }

               let scale = min(image.extent.width / viewSize.width, image.extent.height / viewSize.height)
               let offsetX = (image.extent.width - viewSize.width * scale) / 2.0
               let offsetY = (image.extent.height - viewSize.height * scale) / 2.0
               let length = min(image.extent.width, image.extent.height)
               
               self.elapsedTime = currentMediaTime
               
               let (peekedImage, peekedImageHash) = await Task.detached {
                  let context = CIContext()
                  var tuple: (CGImage?, UInt64) = (nil, 0)
                  
                  if let i = context.createCGImage(image, from: image.extent), let croppedImage = i.cropping(to: CGRect(x: Int(offsetX), y: Int(offsetY), width: Int(length), height: Int(length))), let hash = self.computeHash(image: i) {
                     tuple = (croppedImage, hash)
                  }

                  return tuple
               }.value

               let isCurrent = self.frameState.withLock { state in
                  state.generation == generation
               }

               guard isCurrent && self.isViewVisible && !self.isPaused && self.isReady else {
                  return
               }

               if self.hammingDistance(self.peekedImageHash, peekedImageHash) > self.threshold {
                  self.peekedImage = peekedImage
                  self.peekedImageHash = peekedImageHash
                  self.delegate?.peekDidUpdate(self)
               }
            }
         }
      }
      
      func flash() {
         final class BeamView: UIView, @MainActor CAAnimationDelegate {
            let widthRatio = 0.25
            let skewAngle = 45.0
            let startTime = CACurrentMediaTime()
            let duration = 1.0
            let gradientLayer = CAGradientLayer()
            
            override func layoutSubviews() {
               super.layoutSubviews()
               
               CATransaction.begin()
               CATransaction.setDisableActions(true)
               
               self.gradientLayer.frame = self.bounds
               
               CATransaction.commit()
               
               if let superview = self.superview, self.layer.animation(forKey: "flash") != nil {
                  let skewTransform = CATransform3DMakeAffineTransform(CGAffineTransform(a: 1.0, b: 0.0, c: -tan(self.skewAngle * .pi / 180.0), d: 1.0, tx: 0.0, ty: 0.0))
                  let progress = min((CACurrentMediaTime() - self.startTime) / self.duration, 1.0)
                  
                  CATransaction.begin()
                  
                  let animation = CABasicAnimation(keyPath: "transform")
                  
                  animation.fromValue = CATransform3DConcat(skewTransform, CATransform3DMakeTranslation(floor(superview.bounds.height * abs(tan(self.skewAngle * .pi / 180.0)) / 2.0 - self.bounds.width - superview.bounds.height * abs(tan(self.skewAngle * .pi / 180.0)) + (self.bounds.width + superview.bounds.height * abs(tan(self.skewAngle * .pi / 180.0)) + superview.bounds.width) * progress), 0.0, 0.0))
                  animation.toValue = CATransform3DConcat(skewTransform, CATransform3DMakeTranslation(floor(superview.bounds.height * abs(tan(self.skewAngle * .pi / 180.0)) / 2.0 + superview.bounds.width), 0.0, 0.0))
                  animation.beginTime = CACurrentMediaTime()
                  animation.duration = self.duration * (1.0 - progress)
                  animation.timingFunction = CAMediaTimingFunction(name: .linear)
                  animation.isRemovedOnCompletion = false
                  animation.fillMode = .forwards
                  animation.delegate = self
                  
                  self.layer.add(animation, forKey: "flash")
                  
                  CATransaction.commit()
               }
            }
            
            func animationDidStop(_ anim: CAAnimation, finished flag: Bool) {
               if anim === self.layer.animation(forKey: "flash") {
                  if let superview = self.superview {
                     for constraint in superview.constraints {
                        if constraint.firstItem === self && constraint.secondItem === superview {
                           superview.removeConstraint(constraint)
                        }
                     }
                  }
                  
                  self.removeFromSuperview()
                  self.layer.removeAllAnimations()
               }
            }
         }
         
         let beamView = BeamView()
         let skewTransform = CATransform3DMakeAffineTransform(CGAffineTransform(a: 1.0, b: 0.0, c: -tan(beamView.skewAngle * .pi / 180.0), d: 1.0, tx: 0.0, ty: 0.0))
         
         beamView.gradientLayer.colors = [UIColor.black.withAlphaComponent(0.0).cgColor, UIColor.black.cgColor, UIColor.black.withAlphaComponent(0.0).cgColor]
         beamView.gradientLayer.locations = [0.0, 0.5, 1.0]
         beamView.gradientLayer.startPoint = CGPoint(x: 0.0, y: 0.5)
         beamView.gradientLayer.endPoint = CGPoint(x: 1.0, y: 0.5)
         
         beamView.translatesAutoresizingMaskIntoConstraints = false
         beamView.isUserInteractionEnabled = false
         beamView.backgroundColor = UIColor.white.withAlphaComponent(0.5)
         beamView.layer.mask = beamView.gradientLayer
         
         self.view.addSubview(beamView)
         self.view.addConstraint(NSLayoutConstraint(item: beamView, attribute: .leading, relatedBy: .equal, toItem: self.view, attribute: .leading, multiplier: 1.0, constant: 0.0))
         self.view.addConstraint(NSLayoutConstraint(item: beamView, attribute: .top, relatedBy: .equal, toItem: self.view, attribute: .top, multiplier: 1.0, constant: 0.0))
         self.view.addConstraint(NSLayoutConstraint(item: beamView, attribute: .width, relatedBy: .equal, toItem: self.view, attribute: .width, multiplier: beamView.widthRatio, constant: 0.0))
         self.view.addConstraint(NSLayoutConstraint(item: beamView, attribute: .height, relatedBy: .equal, toItem: self.view, attribute: .height, multiplier: 1.0, constant: 0.0))
         
         CATransaction.begin()
         
         let animation = CABasicAnimation(keyPath: "transform")
         
         animation.fromValue = CATransform3DConcat(skewTransform, CATransform3DMakeTranslation(floor(self.view.bounds.height * abs(tan(beamView.skewAngle * .pi / 180.0)) / 2.0 - self.view.bounds.width * beamView.widthRatio - self.view.bounds.height * abs(tan(beamView.skewAngle * .pi / 180.0))), 0.0, 0.0))
         animation.toValue = CATransform3DConcat(skewTransform, CATransform3DMakeTranslation(floor(self.view.bounds.height * abs(tan(beamView.skewAngle * .pi / 180.0)) / 2.0 + self.view.bounds.width), 0.0, 0.0))
         animation.beginTime = beamView.startTime
         animation.duration = beamView.duration
         animation.timingFunction = CAMediaTimingFunction(name: .linear)
         animation.isRemovedOnCompletion = false
         animation.fillMode = .forwards
         animation.delegate = beamView
         
         beamView.layer.add(animation, forKey: "flash")
         
         CATransaction.commit()
      }
      
      nonisolated func computeHash(image: CGImage) -> UInt64? {
         let size = CGSize(width: 8, height: 8)
         let rendererFormat = UIGraphicsImageRendererFormat()
         
         rendererFormat.opaque = false
         rendererFormat.scale = 1.0
         rendererFormat.preferredRange = .standard
         
         let renderer = UIGraphicsImageRenderer(size: size, format: rendererFormat)
         let resizedImage = renderer.image { rendererContext in
            let context = rendererContext.cgContext
            
            context.interpolationQuality = .high
            context.setAllowsAntialiasing(true)
            context.clear(CGRect(x: 0.0, y: 0.0, width: size.width, height: size.height))
            context.translateBy(x: 0.0, y: size.height)
            context.scaleBy(x: 1.0, y: -1.0)
            context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: size.width, height: size.height))
         }.cgImage
         
         if let resizedImage {
            var pixelData = [UInt8](repeating: 0, count: resizedImage.width * resizedImage.height)
            
            if let context = CGContext(data: &pixelData, width: resizedImage.width, height: resizedImage.height, bitsPerComponent: 8, bytesPerRow: resizedImage.width, space: CGColorSpaceCreateDeviceGray(), bitmapInfo: CGImageAlphaInfo.none.rawValue) {
               context.draw(resizedImage, in: CGRect(x: 0, y: 0, width: resizedImage.width, height: resizedImage.height))
               let sum = pixelData.reduce(0, { $0 + Int($1) })
               let mean = sum / pixelData.count
               var hash: UInt64 = 0
               
               for (i, pixel) in pixelData.enumerated() {
                  if pixel > mean {
                     hash |= 1 << UInt64(63 - i)
                  }
               }
               
               return hash
            }
         }
         
         return nil
      }
      
      func hammingDistance(_ hash1: UInt64, _ hash2: UInt64) -> Int {
         return (hash1 ^ hash2).nonzeroBitCount
      }
   }
}

struct Activity: View {
   private let accent: UIColor
   @Binding private var words: [Word]
   private let scores: [String: (String, Double, [String]?, Date)]
   private let characters: [(name: String, language: String?, sequences: [Sequence])]
   @Binding private var logs: [(id: UUID?, from: String?, to: String?, group: Double, raw: String?, content: (text: String?, image: CGImage?), choices: [String]?)]
   @Environment(\.dismiss) private var dismiss
   @Namespace private var topID
   @State private var mode = 0
   @State private var stats: [Int]? = nil
   @State private var mean: Double? = nil
   @State private var variance: Double? = nil
   @State private var achievements: [String]? = nil
   @State private var remains: [Int?]? = nil
   @State private var trendings: [String]? = nil
   @State private var indexes: [Int]? = nil
   @State private var contents: [(name: String?, text: String?, image: CGImage?)]? = nil
   
   var body: some View {
      NavigationStack {
         ScrollViewReader { proxy in
            List {
               EmptyView()
                  .id(self.topID)
               
               if self.mode == 0 {
                  if self.stats != nil {
                     self.makeStats()
                  }
                  
                  if let achievements = self.achievements, !achievements.isEmpty {
                     self.makeAchievements()
                  }
               } else if self.mode == 1 {
                  if self.trendings != nil {
                     self.makeTrendings()
                  }
               } else {
                  if self.contents != nil {
                     self.makeLogs()
                  }
               }
            }
            .frame(
               maxWidth: .infinity,
               maxHeight: .infinity
            )
            .background(.clear)
            .scrollContentBackground(.hidden)
            .listStyle(DefaultListStyle())
            .navigationBarTitleDisplayMode(.inline)
            .toolbarBackground(.hidden, for: .navigationBar)
            .toolbar {
               ToolbarItem(placement: .principal) {
                  Text("Activity")
                     .foregroundStyle(.primary)
                     .font(.headline)
                     .fontWeight(.semibold)
                     .lineLimit(1)
                     .textCase(.uppercase)
               }
               ToolbarItem(placement: .cancellationAction) {
                  Button(action: {
                     dismiss()
                  }) {
                     ZStack {
                        Image(systemName: "xmark")
                           .frame(
                              alignment: .center
                           )
                           .background(.clear)
                           .foregroundStyle(.primary)
                           .font(
                              .system(size: 8.0)
                           )
                           .bold()
                     }
                  }
                  .contentShape(Circle())
               }
               ToolbarItem(placement: .primaryAction) {
                  Button(action: {
                     withAnimation {
                        proxy.scrollTo(self.topID, anchor: .bottom)
                        self.mode = (self.mode + 1) % 3
                     }
                  }) {
                     ZStack {
                        Image(systemName: "arrow.left.arrow.right")
                           .frame(
                              alignment: .center
                           )
                           .background(.clear)
                           .foregroundStyle(.primary)
                           .font(
                              .system(size: 8.0)
                           )
                           .bold()
                     }
                  }
                  .contentShape(Circle())
               }
            }
            .transition(.opacity)
            .task {
               let (stats, mean, variance, achievements, remains, trendings, indexes, contents) = await self.load()
               
               withAnimation {
                  self.stats = stats
                  self.mean = mean
                  self.variance = variance
                  self.achievements = achievements
                  self.remains = remains
                  self.trendings = trendings.reduce(into: [], { x, y in
                     x.append(y.0)
                  })
                  self.indexes = indexes
                  self.contents = contents
               }
            }
         }
      }
   }
   
   init(accent: UIColor, words: Binding<[Word]>, scores: [String: (String, Double, [String]?, Date)], characters: [(name: String, language: String?, sequences: [Sequence])], logs: Binding<[(id: UUID?, from: String?, to: String?, group: Double, raw: String?, content: (text: String?, image: CGImage?), choices: [String]?)]>) {
      self.accent = accent
      self._words = words
      self.scores = scores
      self.characters = characters
      self._logs = logs
   }
   
   private func makeStats() -> some View {
      return Section(header: Text("Stats")
         .foregroundStyle(.primary)
            .fontWeight(.semibold)
            .lineLimit(1)
            .textCase(.uppercase)) {
               if let stats = self.stats {
                  Chart {
                     let today = Calendar.current.startOfDay(for: Date())
                     
                     ForEach(Array(stats.enumerated()), id: \.offset) { (index, item) in
                        BarMark(
                           x: .value("Time", Calendar.current.date(byAdding: .day, value: -stats.count + 1 + index, to: today)!),
                           y: .value("Stars", item),
                           width: 8
                        )
                        .annotation(position: .top, alignment: .center) {
                           if item > 0 {
                              Text(String(item))
                                 .font(.caption)
                           }
                        }
                        .foregroundStyle(Color(uiColor: self.accent))
                        .clipShape(Capsule())
                     }
                  }
                  .chartXAxis {
                     AxisMarks(preset: .aligned, values: .stride(by: .day, count: 1)) { _ in
                        AxisGridLine()
                        AxisTick()
                        AxisValueLabel(format: .dateTime.weekday())
                     }
                  }
                  .chartYAxis {
                     AxisMarks(position: .leading, values: .automatic) { _ in
                        AxisGridLine()
                        AxisTick()
                     }
                  }
                  .containerRelativeFrame(.vertical) { length, _ in length / 2.0 }
                  .listRowBackground(Color(uiColor: .systemBackground))
                  .transition(.opacity.animation(.linear))
               }
               
               if let mean = self.mean {
                  HStack(alignment: .center, spacing: 16.0) {
                     Text("Mean")
                        .foregroundStyle(.primary)
                        .font(.subheadline)
                        .fontWeight(.semibold)
                     Spacer()
                     Text(String(format: "%.1f", mean))
                        .foregroundStyle(Color(uiColor: self.accent))
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .lineLimit(1)
                  }
                  .listRowBackground(Color(uiColor: .systemBackground))
                  .transition(.opacity.animation(.linear))
               }
               
               if let variance = self.variance {
                  HStack(alignment: .center, spacing: 16.0) {
                     Text("Variance")
                        .foregroundStyle(.primary)
                        .font(.subheadline)
                        .fontWeight(.semibold)
                     Spacer()
                     Text(String(format: "%.1f", variance))
                        .foregroundStyle(Color(uiColor: self.accent))
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .lineLimit(1)
                  }
                  .listRowBackground(Color(uiColor: .systemBackground))
                  .transition(.opacity.animation(.linear))
               }
            }
   }
   
   private func makeAchievements() -> some View {
      return Section(header: Text("Achievements")
         .foregroundStyle(.primary)
            .fontWeight(.semibold)
            .lineLimit(1)
            .textCase(.uppercase)) {
               if let achievements = self.achievements, let remains = self.remains {
                  HStack(alignment: .center, spacing: 16.0) {
                     Text("Overall")
                        .foregroundStyle(.primary)
                        .font(.subheadline)
                        .fontWeight(.semibold)
                     Spacer()
                     Text(String(format: "%.0f%%", Double(achievements.enumerated().reduce(0, { remains[$1.offset] == nil ? $0 + 1 : $0 })) / Double(achievements.count) * 100.0))
                        .foregroundStyle(Color(uiColor: self.accent))
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .lineLimit(1)
                  }
                  .listRowBackground(Color(uiColor: .systemBackground))
                  .transition(.opacity.animation(.linear))
                  ForEach(Array(achievements.enumerated()), id: \.element) { (index, item) in
                     HStack(alignment: .center, spacing: 16.0) {
                        Text(item)
                           .foregroundStyle(.primary)
                           .font(.subheadline)
                           .fontWeight(.semibold)
                        Spacer()
                        
                        if let count = remains[index] {
                           HStack(alignment: .center, spacing: 8.0) {
                              Image(systemName: "lock")
                                 .frame(
                                    width: 16.0,
                                    height: 16.0,
                                    alignment: .center
                                 )
                                 .background(.clear)
                                 .foregroundStyle(Color(uiColor: self.accent))
                                 .font(
                                    .system(size: 16.0)
                                 )
                                 .bold()
                              
                              if count > 0 {
                                 Text(String(format: "%ld", -count))
                                    .foregroundStyle(Color(uiColor: self.accent))
                                    .font(.subheadline)
                                    .fontWeight(.semibold)
                                    .lineLimit(1)
                              } else {
                                 HStack(alignment: .center, spacing: 8.0) {
                                    Image(systemName: "greaterthan")
                                       .background(.clear)
                                       .foregroundStyle(Color(uiColor: self.accent))
                                       .font(
                                          .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                                       )
                                    Text(String(format: "%ld", 10))
                                       .foregroundStyle(Color(uiColor: self.accent))
                                       .font(.subheadline)
                                       .fontWeight(.semibold)
                                       .lineLimit(1)
                                 }
                              }
                           }
                        } else {
                           Image(systemName: "lock.open")
                              .frame(
                                 width: 16.0,
                                 height: 16.0,
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(Color(uiColor: self.accent))
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                        }
                     }
                     .listRowBackground(Color(uiColor: .systemBackground))
                  }
                  .transition(.opacity.animation(.linear))
               }
            }
   }
   
   private func makeTrendings() -> some View {
      return Section(header: Text("Trending")
         .foregroundStyle(.primary)
            .fontWeight(.semibold)
            .lineLimit(1)
            .textCase(.uppercase)) {
               if let trendings = self.trendings {
                  if trendings.isEmpty {
                     Text("None")
                        .foregroundStyle(.primary)
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .frame(
                           maxWidth: .infinity,
                           alignment: .center
                        )
                        .listRowBackground(Color(uiColor: .systemBackground))
                        .transition(.opacity.animation(.linear))
                  } else {
                     ForEach(Array(trendings.enumerated()), id: \.element) { (index, word) in
                        if self.words.contains(where: { $0.name.compare(word, options: [.caseInsensitive]) == .orderedSame }) {
                           HStack(alignment: .center, spacing: 16.0) {
                              Text(String(format: "%ld", index + 1))
                                 .foregroundStyle(.primary)
                                 .font(.subheadline)
                                 .fontWeight(.semibold)
                                 .lineLimit(1)
                              Text(word)
                                 .foregroundStyle(.primary)
                                 .font(.subheadline)
                                 .fontWeight(.semibold)
                                 .lineLimit(1)
                              
                              if index == 0 {
                                 Image(systemName: "crown")
                                    .frame(
                                       width: 16.0,
                                       height: 16.0,
                                       alignment: .center
                                    )
                                    .background(.clear)
                                    .foregroundStyle(Color(uiColor: self.accent))
                                    .font(
                                       .system(size: 16.0)
                                    )
                                    .bold()
                              }
                           }
                           .listRowBackground(Color(uiColor: .systemBackground))
                           .transition(.opacity.animation(.linear))
                        } else {
                           Button(action: {
                              withAnimation {
                                 self.words.append(Word(name: word, attributes: nil))
                              }
                              
                              self.save(words: self.words)
                           }) {
                              HStack(alignment: .center, spacing: 16.0) {
                                 Text(String(format: "%ld", index + 1))
                                    .foregroundStyle(.primary)
                                    .font(.subheadline)
                                    .fontWeight(.semibold)
                                    .lineLimit(1)
                                 Text(word)
                                    .foregroundStyle(.primary)
                                    .font(.subheadline)
                                    .fontWeight(.semibold)
                                    .lineLimit(1)
                                 
                                 if index == 0 {
                                    Image(systemName: "crown")
                                       .frame(
                                          width: 16.0,
                                          height: 16.0,
                                          alignment: .center
                                       )
                                       .background(.clear)
                                       .foregroundStyle(Color(uiColor: self.accent))
                                       .font(
                                          .system(size: 16.0)
                                       )
                                       .bold()
                                 }
                                 
                                 Spacer()
                                 Image(systemName: "plus")
                                    .frame(
                                       alignment: .center
                                    )
                                    .background(.clear)
                                    .foregroundStyle(Color(uiColor: self.accent))
                                    .font(
                                       .system(size: 16.0)
                                    )
                                    .bold()
                              }
                              .frame(
                                 maxWidth: .infinity
                              )
                              .contentShape(Rectangle())
                           }
                           .buttonStyle(PlainButtonStyle())
                           .disabled(self.words.contains(where: { $0.name.compare(word, options: [.caseInsensitive]) == .orderedSame }))
                           .listRowBackground(Color(uiColor: .systemBackground))
                           .transition(.opacity.animation(.linear))
                        }
                     }
                  }
               }
            }
   }
   
   private func makeLogs() -> some View {
      return Section(header: Text("Logs")
         .foregroundStyle(.primary)
            .fontWeight(.semibold)
            .lineLimit(1)
            .textCase(.uppercase)) {
               if var indexes = self.indexes, var contents = self.contents {
                  if contents.isEmpty {
                     Text("None")
                        .foregroundStyle(.primary)
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .frame(
                           maxWidth: .infinity,
                           alignment: .center
                        )
                        .listRowBackground(Color(uiColor: .systemBackground))
                        .transition(.opacity.animation(.linear))
                  } else {
                     ForEach(Array(indexes.reversed().enumerated()), id: \.element) { (_, index) in
                        if let name = contents[index].name {
                           VStack(alignment: .leading, spacing: 8.0) {
                              HStack(alignment: .center, spacing: 8.0) {
                                 Text(name)
                                    .foregroundStyle(Color(uiColor: self.accent))
                                    .font(.subheadline)
                                    .fontWeight(.semibold)
                                    .lineLimit(1)
                                 Image(systemName: "arrow.down.left")
                                    .frame(
                                       width: 16.0,
                                       height: 16.0,
                                       alignment: .center
                                    )
                                    .background(.clear)
                                    .foregroundStyle(Color(uiColor: self.accent))
                                    .font(
                                       .system(size: 16.0)
                                    )
                                    .bold()
                              }
                              
                              if let text = contents[index].text {
                                 Text(text)
                                    .foregroundStyle(.primary)
                                    .font(.subheadline)
                                    .fontWeight(.semibold)
                                    .frame(
                                       maxWidth: .infinity,
                                       alignment: .leading
                                    )
                                    .multilineTextAlignment(.leading)
                              }
                              
                              if let image = contents[index].image {
                                 Image(uiImage: UIImage(cgImage: image))
                                    .resizable()
                                    .scaledToFill()
                                    .frame(
                                       maxWidth: .infinity,
                                       alignment: .leading
                                    )
                                    .clipShape(RoundedRectangle(cornerRadius: 16.0))
                              }
                           }
                           .listRowBackground(Color(uiColor: .systemBackground))
                        } else {
                           VStack(alignment: .leading, spacing: 8.0) {
                              if let text = contents[index].text, let image = contents[index].image {
                                 HStack(alignment: .center, spacing: 8.0) {
                                    Text(text)
                                       .foregroundStyle(Color(uiColor: self.accent))
                                       .font(.subheadline)
                                       .fontWeight(.semibold)
                                       .lineLimit(1)
                                    Image(systemName: "arrow.up.right")
                                       .frame(
                                          width: 16.0,
                                          height: 16.0,
                                          alignment: .center
                                       )
                                       .background(.clear)
                                       .foregroundStyle(Color(uiColor: self.accent))
                                       .font(
                                          .system(size: 16.0)
                                       )
                                       .bold()
                                 }
                                 Image(uiImage: UIImage(cgImage: image))
                                    .resizable()
                                    .scaledToFill()
                                    .frame(
                                       maxWidth: .infinity,
                                       alignment: .leading
                                    )
                                    .clipShape(RoundedRectangle(cornerRadius: 16.0))
                              } else if let text = contents[index].text {
                                 HStack(alignment: .center, spacing: 8.0) {
                                    Text(text)
                                       .foregroundStyle(Color(uiColor: self.accent))
                                       .font(.subheadline)
                                       .fontWeight(.semibold)
                                       .lineLimit(1)
                                    Image(systemName: "arrow.up.right")
                                       .frame(
                                          width: 16.0,
                                          height: 16.0,
                                          alignment: .center
                                       )
                                       .background(.clear)
                                       .foregroundStyle(Color(uiColor: self.accent))
                                       .font(
                                          .system(size: 16.0)
                                       )
                                       .bold()
                                 }
                              } else if let image = contents[index].image {
                                 ZStack {
                                    Image(uiImage: UIImage(cgImage: image))
                                       .resizable()
                                       .scaledToFill()
                                       .frame(
                                          maxWidth: .infinity,
                                          alignment: .leading
                                       )
                                       .clipShape(RoundedRectangle(cornerRadius: 16.0))
                                    Image(systemName: "arrow.up.right")
                                       .frame(
                                          width: 16.0,
                                          height: 16.0,
                                          alignment: .center
                                       )
                                       .background(.clear)
                                       .foregroundStyle(Color(uiColor: self.accent))
                                       .font(
                                          .system(size: 16.0)
                                       )
                                       .bold()
                                       .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topTrailing)
                                       .offset(x: -8.0, y: 8.0)
                                 }
                                 .frame(
                                    maxWidth: .infinity
                                 )
                                 .background(.clear)
                              }
                           }
                           .listRowBackground(Color(uiColor: .systemBackground))
                        }
                     }
                     .transition(.opacity.animation(.linear))
                     Button(action: {
                        indexes.removeAll()
                        contents.removeAll()
                        
                        withAnimation {
                           self.indexes = indexes
                           self.contents = contents
                        }
                        
                        self.logs.removeAll()
                     }) {
                        Text("Reset")
                           .foregroundStyle(.primary)
                           .font(.subheadline)
                           .fontWeight(.semibold)
                           .frame(
                              maxWidth: .infinity,
                              alignment: .center
                           )
                           .contentShape(Rectangle())
                     }
                     .buttonStyle(PlainButtonStyle())
                     .listRowBackground(Color(uiColor: .systemBackground))
                     .transition(.opacity.animation(.linear))
                  }
               }
            }
   }
   
   private func load() async -> ([Int], Double, Double, [String], [Int?], [(String, Double)], [Int], [(name: String?, text: String?, image: CGImage?)]) {
      let words = self.words
      let characters = self.characters
      let logs = self.logs
      
      return await Task.detached {
         let maxDays = 6
         let nowDateComponents = Calendar.current.dateComponents([.calendar, .timeZone, .era, .year, .month, .day], from: Date())
         var stats = [Int]()
         var mean = 0.0
         var variance = 0.0
         let stars = words.count
         var achievements = [String]()
         var remains = [Int?]()
         let epsilon: Double = 1e-6
         var trendings = [(String, Double)]()
         var indexes = [Int]()
         var contents = [(name: String?, text: String?, image: CGImage?)]()
         
         for i in stride(from: -maxDays, through: 0, by: 1) {
            let dateComponents = DateComponents(calendar: nowDateComponents.calendar, timeZone: nowDateComponents.timeZone, era: nowDateComponents.era, year: nowDateComponents.year, month: nowDateComponents.month, day: nowDateComponents.day! + i, hour: 0, minute: 0, second: 0, nanosecond: 0)
            var count = 0
            
            for word in words {
               let dc = Calendar.current.dateComponents([.year, .month, .day], from: Date(timeIntervalSince1970: Double(word.timestamp)))
               
               if dateComponents.year == dc.year && dateComponents.month == dc.month && dateComponents.day == dc.day {
                  count += 1
               }
            }
            
            stats.append(count)
         }
         
         mean = self.mean(data: stats)
         variance = self.variance(data: stats, mean: mean)
         
         for character in characters {
            var available = 0
            var max = 0
            var lockedAchievements = [String: Int]()
            var unlockableAchievementSet = Set<String>()
            var tempAchievements = [(name: String, count: Int?)]()
            
            for sequence in character.sequences {
               var isLocked = true
               var requiredStars = 0
               
               if let pattern = sequence.state, let regex = try? Regex(pattern) {
                  for i in 0...stars + 10 {
                     if let match = "\(i)".firstMatch(of: regex), !match.output.isEmpty {
                        if i <= stars {
                           available += 1
                           isLocked = false
                        } else {
                           requiredStars = i - stars
                        }
                        
                        break
                     }
                  }
               }
               
               for i in 0..<sequence.count {
                  if case .sequence(let s1) = sequence[i] {
                     if let name = s1.name, s1.state == nil && !s1.isEmpty {
                        for j in stride(from: i + 1, to: sequence.count, by: 1) {
                           if case .sequence(let s2) = sequence[j] {
                              var isAvailable = false
                              
                              if s2.isEmpty {
                                 if s1.name == s2.name && s2.state == nil {
                                    isAvailable = true
                                 }
                              } else {
                                 var queue = [Sequence]()
                                 
                                 for step in s2 {
                                    if case .sequence(let s3) = step {
                                       queue.append(s3)
                                    }
                                 }
                                 
                                 while !queue.isEmpty {
                                    let s = queue.removeFirst()
                                    
                                    if s.isEmpty {
                                       if s1.name == s.name && s.state == nil {
                                          isAvailable = true
                                       }
                                    } else {
                                       for step in s {
                                          if case .sequence(let s3) = step {
                                             queue.append(s3)
                                          }
                                       }
                                    }
                                 }
                              }
                              
                              if isAvailable {
                                 if isLocked {
                                    if let count = lockedAchievements[name] {
                                       if requiredStars > count {
                                          lockedAchievements[name] = requiredStars
                                       }
                                    } else {
                                       lockedAchievements[name] = requiredStars
                                    }
                                 }
                                 
                                 if !unlockableAchievementSet.contains(name) {
                                    unlockableAchievementSet.insert(name)
                                 }
                                 
                                 break
                              }
                           }
                        }
                     }
                  }
               }
               
               max += 1
            }
            
            for name in unlockableAchievementSet {
               if let count = lockedAchievements[name] {
                  tempAchievements.append((name: name, count: count))
               } else {
                  tempAchievements.append((name: name, count: nil))
               }
            }
            
            tempAchievements.sort { $0.name < $1.name }
            
            for (name, count) in tempAchievements {
               achievements.append(name)
               remains.append(count)
            }
         }
         
         if characters.contains(where: { $0.language == nil }) {
            for (_, value) in self.scores {
               if value.1 > epsilon {
                  trendings.append((value.0, value.1))
               }
            }
         } else {
            for (_, value) in self.scores {
               if value.1 > epsilon {
                  if let languages = value.2 {
                     if languages.contains(where: { language in
                        return characters.contains(where: { $0.language == language })
                     }) {
                        trendings.append((value.0, value.1))
                     }
                  } else {
                     trendings.append((value.0, value.1))
                  }
               }
            }
         }
         
         trendings.sort { $0.1 > $1.1 }
         
         if trendings.count > 10 {
            trendings.removeSubrange(10...)
         }
         
         for (index, log) in logs.enumerated() {
            indexes.append(index)
            contents.append((name: log.from, text: log.content.text, image: log.content.image))
         }
         
         return (stats, mean, variance, achievements, remains, trendings, indexes, contents)
      }.value
   }
   
   private func save(words: [Word]) {
      Task {
         await Task.detached {
            let encoder = JSONEncoder()
            
            if let data = try? encoder.encode(words) {
               if let url = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {
                  let path = url.appending(path: "words.json", directoryHint: .inferFromPath).path(percentEncoded: false)
                  
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
               
               if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
                  let documentsUrl = containerUrl.appending(path: "Documents", directoryHint: .isDirectory)
                  let documentsPath = documentsUrl.path(percentEncoded: false)
                  let url = documentsUrl.appending(path: ".words.json", directoryHint: .inferFromPath)
                  let path = url.path(percentEncoded: false)
                  
                  if !FileManager.default.fileExists(atPath: documentsPath) {
                     try? FileManager.default.createDirectory(atPath: documentsPath, withIntermediateDirectories: false)
                  }
                  
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
                  
                  if let currentVersion = NSFileVersion.currentVersionOfItem(at: url), currentVersion.isConflict {
                     try? NSFileVersion.removeOtherVersionsOfItem(at: url)
                     
                     if let conflictVersions = NSFileVersion.unresolvedConflictVersionsOfItem(at: url) {
                        for fileVersion in conflictVersions {
                           fileVersion.isResolved = true
                        }
                     }
                  }
               }
            }
         }.value
      }
   }
   
   private nonisolated func mean(data: [Int]) -> Double {
      var sum = 0.0
      
      for x in data {
         sum += Double(x)
      }
      
      return sum / Double(data.count)
   }
   
   private nonisolated func variance(data: [Int], mean: Double) -> Double {
      var sum = 0.0
      
      for x in data {
         sum += (Double(x) - mean) * (Double(x) - mean)
      }
      
      return sum / Double(data.count)
   }
}

struct Dictionary: View {
   let active: Bool
   let accent: UIColor
   @Binding var type: String?
   @Binding var words: [Word]
   let attributes: [String]
   @Environment(\.scenePhase) private var scenePhase
   @Environment(\.dismiss) private var dismiss
   @FocusState private var inputFocused: Bool
   @Namespace private var topID
   @State private var isEditing = false
   @State private var isSubmittable = false
   @State private var isRecording = false
   @State private var isCapturing = false
   @State private var isModifier = false
   @State private var input = String()
   @State private var selectedAttributes = Set<String>()
   @State private var path = [Word]()
   @State private var volumeLevel = 0.0
   @State private var audioEngine: AVAudioEngine? = nil
   @State private var speechRecognizer: SFSpeechRecognizer? = nil
   @State private var speechAudioBufferRecognitionRequest: SFSpeechAudioBufferRecognitionRequest? = nil
   @State private var speechRecognitionTask: SFSpeechRecognitionTask? = nil
   
   var body: some View {
      NavigationStack() {
         ScrollViewReader { proxy in
            List {
               EmptyView()
                  .id(self.topID)
               
               if self.path.isEmpty {
                  self.makeNew()
                  self.makeWords(proxy: proxy)
               } else if let word = self.path.first {
                  self.makeAttributes(word: word)
               }
            }
            .frame(
               maxWidth: .infinity,
               maxHeight: .infinity
            )
            .background(.clear)
            .scrollContentBackground(.hidden)
            .listStyle(DefaultListStyle())
            .environment(\.editMode, .constant(self.isEditing ? EditMode.active : EditMode.inactive))
            .navigationBarTitleDisplayMode(.inline)
            .toolbarBackground(.hidden, for: .navigationBar)
            .toolbar {
               ToolbarItem(placement: .principal) {
                  ZStack {
                     Text("Dictionary")
                        .foregroundStyle(.primary)
                        .font(.headline)
                        .fontWeight(.semibold)
                        .lineLimit(1)
                        .textCase(.uppercase)
                        .opacity(self.path.isEmpty ? 1.0 : 0.0)
                     
                     if !self.path.isEmpty {
                        Text(self.path[0].name)
                           .foregroundStyle(.primary)
                           .font(.headline)
                           .fontWeight(.semibold)
                           .lineLimit(1)
                           .textCase(.uppercase)
                           .transition(.opacity)
                     }
                  }
                  .frame(maxWidth: .infinity)
               }
               ToolbarItem(placement: .cancellationAction) {
                  Button(action: {
                     if self.path.isEmpty {
                        dismiss()
                     } else {
                        withAnimation {
                           proxy.scrollTo(self.topID, anchor: .bottom)
                           self.path.removeAll()
                        }
                     }
                  }) {
                     if self.path.isEmpty {
                        ZStack {
                           Image(systemName: "xmark")
                              .frame(
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(.primary)
                              .font(.system(size: 8.0))
                              .bold()
                              .transition(.opacity)
                        }
                     } else {
                        ZStack {
                           Image(systemName: "arrow.backward")
                              .frame(
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(.primary)
                              .font(.system(size: 8.0))
                              .bold()
                              .transition(.opacity)
                        }
                     }
                  }
                  .contentShape(Circle())
               }
               ToolbarItem(placement: .primaryAction) {
                  Button(action: {
                     if self.path.isEmpty {
                        if self.inputFocused {
                           self.inputFocused = false
                           
                           if self.isEditing {
                              withAnimation(.linear(duration: 0.5)) {
                                 self.isEditing = false
                              }
                           }
                        } else {
                           withAnimation(.linear(duration: 0.5)) {
                              self.isEditing.toggle()
                           }
                        }
                     } else if let index = self.words.firstIndex(where: { $0.id == self.path[0].id }) {
                        for attribute in self.attributes {
                           if var attributes = self.words[index].attributes, let i = attributes.firstIndex(where: { $0 == attribute }) {
                              attributes.remove(at: i)
                              
                              withAnimation {
                                 self.words[index].attributes = attributes
                              }
                           }
                        }
                        
                        self.save(words: self.words)
                     }
                  }) {
                     if self.path.isEmpty {
                        if self.isEditing || self.inputFocused {
                           ZStack {
                              Image(systemName: "checkmark")
                                 .frame(
                                    alignment: .center
                                 )
                                 .background(.clear)
                                 .foregroundStyle(.primary)
                                 .font(.system(size: 8.0))
                                 .bold()
                                 .transition(.opacity)
                           }
                        } else {
                           ZStack {
                              Image(systemName: "pencil")
                                 .frame(
                                    alignment: .center
                                 )
                                 .background(.clear)
                                 .foregroundStyle(.primary)
                                 .font(.system(size: 8.0))
                                 .bold()
                                 .transition(.opacity)
                           }
                        }
                     } else {
                        ZStack {
                           Image(systemName: "gobackward")
                              .frame(
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(.primary)
                              .font(.system(size: 8.0))
                              .bold()
                              .transition(.opacity)
                        }
                     }
                  }
                  .contentShape(Circle())
               }
            }
            .sheet(isPresented: self.$isCapturing, content: {
               Camera(active: self.active, text: self.$input)
                  .presentationDetents([.medium, .large])
            })
            .onChange(of: self.scenePhase) {
               guard self.scenePhase == .background else {
                  return
               }
               
               if self.isRecording {
                  self.stopRecognize()
               }
               
               self.isCapturing = false
            }
            .onChange(of: self.type) {
               if let type = self.type {
                  self.input = type
                  self.type = nil
               }
            }
            .onChange(of: self.words) {
               if let word = self.path.first, let index = self.words.firstIndex(where: { $0.id == word.id }), let attributes = self.words[index].attributes, attributes.isEmpty {
                  self.isModifier = true
               } else {
                  self.isModifier = false
               }
            }
            .onChange(of: self.path) {
               if let word = self.path.first, let attributes = word.attributes, attributes.isEmpty {
                  self.isModifier = true
               } else {
                  self.isModifier = false
               }
            }
            .onAppear {
               if let type = self.type {
                  self.input = type
                  self.type = nil
               }
            }
         }
      }
   }
   
   init(active: Bool, accent: UIColor, type: Binding<String?>, words: Binding<[Word]>, attributes: [String]) {
      self.active = active
      self.accent = accent
      self._type = type
      self._words = words
      self.attributes = attributes
   }
   
   private func makeNew() -> some View {
      Section(header: Text("New")
         .foregroundStyle(.primary)
         .fontWeight(.semibold)
         .lineLimit(1)
         .textCase(.uppercase)) {
            HStack(alignment: .center, spacing: 16.0) {
               TextField("Word", text: self.$input)
                  .foregroundStyle(.primary)
                  .font(.subheadline)
                  .fontWeight(.semibold)
                  .submitLabel(.done)
                  .focused(self.$inputFocused)
                  .textInputAutocapitalization(.never)
                  .disableAutocorrection(true)
                  .background(.clear)
                  .tint(Color(uiColor: self.accent))
                  .onChange(of: self.input) {
                     if self.input.isEmpty {
                        withAnimation(.linear(duration: 0.5)) {
                           self.isSubmittable = false
                        }
                     } else {
                        withAnimation(.linear(duration: 0.5)) {
                           self.isSubmittable = true
                        }
                     }
                  }
                  .onChange(of: self.inputFocused) {
                     if self.inputFocused && self.isRecording {
                        self.stopRecognize()
                     }
                  }
                  .onChange(of: self.isRecording) {
                     if self.isRecording && self.inputFocused {
                        self.inputFocused = false
                     }
                  }
                  .onChange(of: self.isCapturing) {
                     if self.isCapturing && self.isRecording {
                        self.stopRecognize()
                     }
                  }
                  .onSubmit {
                     if self.input.isEmpty {
                        self.inputFocused = false
                     } else {
                        Task {
                           let input = self.input
                           
                           await Task.detached {
                              await MainActor.run {
                                 self.inputFocused = false
                                 self.input = String()
                              }
                           }.value
                           
                           await Task.detached {
                              await MainActor.run {
                                 withAnimation {
                                    self.words.append(Word(name: input, attributes: nil))
                                 }
                                 
                                 self.save(words: self.words)
                              }
                           }.value
                        }
                     }
                  }
                  .onDisappear {
                     if self.isRecording {
                        self.stopRecognize()
                     }
                  }
               Button(action: {
                  if self.isRecording {
                     self.stopRecognize()
                  } else {
                     self.startRecognize()
                  }
               }) {
                  ZStack {
                     Image(systemName: "mic")
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .foregroundStyle(Color(uiColor: self.accent))
                        .font(
                           .system(size: 16.0)
                        )
                        .bold()
                        .opacity(self.isRecording ? 0.5 + 0.5 * (1.0 - self.volumeLevel) : 0.0)
                        .transition(.opacity)
                     Image(systemName: "mic")
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .foregroundStyle(.primary)
                        .font(
                           .system(size: 16.0)
                        )
                        .bold()
                        .opacity(self.isRecording ? 0.0 : 1.0)
                        .transition(.opacity)
                  }
                  .background(.clear)
               }
               .buttonStyle(PlainButtonStyle())
               .frame(
                  width: 24.0,
                  height: 24.0,
                  alignment: .center
               )
               .background(.clear)
               Button(action: {
                  self.isCapturing = true
               }) {
                  ZStack {
                     Image(systemName: "camera")
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .foregroundStyle(Color(uiColor: self.accent))
                        .font(
                           .system(size: 16.0)
                        )
                        .bold()
                        .opacity(self.isCapturing ? 1.0 : 0.0)
                        .transition(.opacity)
                     Image(systemName: "camera")
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .foregroundStyle(.primary)
                        .font(
                           .system(size: 16.0)
                        )
                        .bold()
                        .opacity(self.isCapturing ? 0.0 : 1.0)
                        .transition(.opacity)
                  }
                  .background(.clear)
               }
               .buttonStyle(PlainButtonStyle())
               .frame(
                  width: 24.0,
                  height: 24.0,
                  alignment: .center
               )
               .background(.clear)
               Button(action: {
                  Task {
                     let input = self.input
                     
                     await Task.detached {
                        await MainActor.run {
                           self.inputFocused = false
                           self.input = String()
                        }
                     }.value
                     
                     await Task.detached {
                        await MainActor.run {
                           withAnimation {
                              self.words.append(Word(name: input, attributes: nil))
                           }
                           
                           self.save(words: self.words)
                        }
                     }.value
                  }
               }) {
                  Image(systemName: "plus")
                     .frame(
                        alignment: .center
                     )
                     .background(.clear)
                     .foregroundStyle(.primary)
                     .font(
                        .system(size: 16.0)
                     )
                     .bold()
               }
               .buttonStyle(PlainButtonStyle())
               .disabled(!self.isSubmittable)
               .frame(
                  width: 24.0,
                  height: 24.0,
                  alignment: .center
               )
               .background(.clear)
            }
            .listRowBackground(Color(uiColor: .systemBackground))
            .transition(.opacity.animation(.linear))
         }
   }
   
   private func makeWords(proxy: ScrollViewProxy) -> some View {
      return Section(header: Text("Words")
         .foregroundStyle(.primary)
         .fontWeight(.semibold)
         .lineLimit(1)
         .textCase(.uppercase)) {
            if self.words.isEmpty {
               Text("None")
                  .foregroundStyle(.primary)
                  .font(.subheadline)
                  .fontWeight(.semibold)
                  .frame(
                     maxWidth: .infinity,
                     alignment: .center
                  )
                  .listRowBackground(Color(uiColor: .systemBackground))
                  .transition(.opacity.animation(.linear))
            } else {
               ForEach(Array(self.words.reversed().enumerated()), id: \.element) { (index, word) in
                  if self.attributes.isEmpty {
                     Text(word.name)
                        .foregroundStyle(.primary)
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .listRowBackground(Color(uiColor: .systemBackground))
                  } else {
                     Button(action: {
                        withAnimation {
                           proxy.scrollTo(self.topID, anchor: .bottom)
                           self.path.append(word)
                        }
                     }) {
                        HStack(alignment: .center, spacing: 16.0) {
                           Text(word.name)
                              .foregroundStyle(.primary)
                              .font(.subheadline)
                              .fontWeight(.semibold)
                           Spacer()
                           
                           if let attributes = word.attributes {
                              if attributes.isEmpty {
                                 Text("Modifier")
                                    .foregroundStyle(Color(uiColor: self.accent))
                                    .font(.caption)
                                    .fontWeight(.semibold)
                                    .lineLimit(1)
                                    .textCase(.uppercase)
                                    .padding(EdgeInsets(
                                       top: 4.0,
                                       leading: 8.0,
                                       bottom: 4.0,
                                       trailing: 8.0
                                    ))
                                    .overlay(
                                       Capsule(style: .continuous)
                                          .stroke(Color(uiColor: self.accent), style: StrokeStyle(lineWidth: 1.0))
                                    )
                              } else {
                                 Text(String(format: "%ld", attributes.count))
                                    .foregroundStyle(Color(uiColor: self.accent))
                                    .font(.caption)
                                    .fontWeight(.semibold)
                                    .lineLimit(1)
                                    .padding(EdgeInsets(
                                       top: 4.0,
                                       leading: 8.0,
                                       bottom: 4.0,
                                       trailing: 8.0
                                    ))
                                    .overlay(
                                       Capsule(style: .continuous)
                                          .stroke(Color(uiColor: self.accent), style: StrokeStyle(lineWidth: 1.0))
                                    )
                              }
                           }
                           
                           Image(systemName: "chevron.right")
                              .frame(
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(Color(uiColor: self.accent))
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                        }
                        .frame(
                           maxWidth: .infinity
                        )
                        .contentShape(Rectangle())
                     }
                     .buttonStyle(PlainButtonStyle())
                     .listRowBackground(Color(uiColor: .systemBackground))
                  }
               }.onDelete(perform: { indexSet in
                  var offsets = IndexSet()
                  
                  for index in indexSet {
                     offsets.insert(self.words.count - 1 - index)
                  }
                  
                  withAnimation {
                     self.words.remove(atOffsets: offsets)
                  }
                  
                  self.save(words: self.words)
               }).onMove(perform: { indices, newOffset in
                  var offsets = IndexSet()
                  
                  for index in indices {
                     offsets.insert(self.words.count - 1 - index)
                  }
                  
                  self.words.move(fromOffsets: offsets, toOffset: self.words.count - newOffset)
                  self.save(words: self.words)
               })
               .transition(.opacity.animation(.linear))
            }
         }
   }
   
   @ViewBuilder
   private func makeAttributes(word: Word) -> some View {
      if self.attributes.isEmpty {
         Section(header: Text("Attributes")
            .foregroundStyle(.primary)
            .fontWeight(.semibold)
            .lineLimit(1)
            .textCase(.uppercase)) {
               Toggle("Modifier", isOn: Binding<Bool>(get: { self.isModifier }, set: { _ in
                  if let index = self.words.firstIndex(where: { $0.id == word.id }) {
                     if self.words[index].attributes == nil {
                        withAnimation {
                           self.words[index].attributes = []
                        }
                     } else {
                        withAnimation {
                           self.words[index].attributes = nil
                        }
                     }
                     
                     self.save(words: self.words)
                  }
               }))
               .foregroundStyle(.primary)
               .font(.subheadline)
               .fontWeight(.semibold)
               .tint(Color(self.accent))
               .listRowBackground(Color(uiColor: .systemBackground))
            }
      } else {
         Section(header: Text("Attributes")
            .foregroundStyle(.primary)
            .fontWeight(.semibold)
            .lineLimit(1)
            .textCase(.uppercase)) {
               ForEach(self.attributes, id: \.self) { attribute in
                  if let index = self.words.firstIndex(where: { $0.id == word.id }) {
                     Button(action: {
                        if var attributes = self.words[index].attributes {
                           if let i = attributes.firstIndex(where: { $0 == attribute }) {
                              attributes.remove(at: i)
                           } else {
                              attributes.append(attribute)
                           }
                           
                           withAnimation {
                              self.words[index].attributes = attributes
                           }
                           
                           self.save(words: self.words)
                        } else {
                           withAnimation {
                              self.words[index].attributes = [attribute]
                           }
                           
                           self.save(words: self.words)
                        }
                     }) {
                        HStack(alignment: .center, spacing: 16.0) {
                           Text(attribute)
                              .foregroundStyle(.primary)
                              .font(.subheadline)
                              .fontWeight(.semibold)
                           Spacer()
                           Image(systemName: "checkmark")
                              .frame(
                                 width: 16.0,
                                 height: 16.0,
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(Color(self.accent))
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                              .opacity((self.words[index].attributes ?? []).contains(attribute) ? 1.0 : 0.0)
                              .animation(.linear(duration: 0.5), value: (self.words[index].attributes ?? []).contains(attribute))
                        }
                        .frame(
                           maxWidth: .infinity
                        )
                        .contentShape(Rectangle())
                     }
                     .buttonStyle(PlainButtonStyle())
                     .listRowBackground(Color(uiColor: .systemBackground))
                  }
               }
               .transition(.opacity.animation(.linear))
            }
         Section {
            Toggle("Modifier", isOn: Binding<Bool>(get: { self.isModifier }, set: { _ in
               if let index = self.words.firstIndex(where: { $0.id == word.id }) {
                  if self.words[index].attributes == nil {
                     withAnimation {
                        self.words[index].attributes = []
                     }
                  } else {
                     withAnimation {
                        self.words[index].attributes = nil
                     }
                  }
                  
                  self.save(words: self.words)
               }
            }))
            .foregroundStyle(.primary)
            .font(.subheadline)
            .fontWeight(.semibold)
            .tint(Color(self.accent))
            .listRowBackground(Color(uiColor: .systemBackground))
         }
      }
   }
   
   private func save(words: [Word]) {
      Task {
         await Task.detached {
            let encoder = JSONEncoder()
            
            if let data = try? encoder.encode(words) {
               if let url = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first {
                  let path = url.appending(path: "words.json", directoryHint: .inferFromPath).path(percentEncoded: false)
                  
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
               
               if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
                  let documentsUrl = containerUrl.appending(path: "Documents", directoryHint: .isDirectory)
                  let documentsPath = documentsUrl.path(percentEncoded: false)
                  let url = documentsUrl.appending(path: ".words.json", directoryHint: .inferFromPath)
                  let path = url.path(percentEncoded: false)
                  
                  if !FileManager.default.fileExists(atPath: documentsPath) {
                     try? FileManager.default.createDirectory(atPath: documentsPath, withIntermediateDirectories: false)
                  }
                  
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
                  
                  if let currentVersion = NSFileVersion.currentVersionOfItem(at: url), currentVersion.isConflict {
                     try? NSFileVersion.removeOtherVersionsOfItem(at: url)
                     
                     if let conflictVersions = NSFileVersion.unresolvedConflictVersionsOfItem(at: url) {
                        for fileVersion in conflictVersions {
                           fileVersion.isResolved = true
                        }
                     }
                  }
               }
            }
         }.value
      }
   }
   
   private func startRecognize() {
      guard let recognizer = SFSpeechRecognizer() else {
         return
      }
      
      withAnimation(.linear(duration: 0.5)) {
         self.isRecording = true
      }
      
      let input = self.input
      
      self.speechRecognizer = recognizer
      
      Task {
         let speechAllowed: Bool
         let authorizationStatus = SFSpeechRecognizer.authorizationStatus()
         
         if authorizationStatus == .notDetermined {
            speechAllowed = await withCheckedContinuation(isolation: nil) { @Sendable (continuation: CheckedContinuation<Bool, Never>) in
               SFSpeechRecognizer.requestAuthorization { status in
                  continuation.resume(returning: status == .authorized)
               }
            }
         } else {
            speechAllowed = authorizationStatus == .authorized
         }
         
         guard self.isRecording, self.speechRecognizer === recognizer else {
            return
         }
         
         guard speechAllowed else {
            self.speechRecognizer = nil
            self.isRecording = false
            
            return
         }
         
         let microphoneAllowed = await AVAudioApplication.requestRecordPermission()
         
         guard self.isRecording, self.speechRecognizer === recognizer else {
            return
         }
         
         guard microphoneAllowed else {
            self.speechRecognizer = nil
            self.isRecording = false
            
            return
         }
         
         let audioSessionActivated: Bool
         
         do {
            let audioSession = AVAudioSession.sharedInstance()
            
            if audioSession.category != .playAndRecord || audioSession.mode != .measurement {
               try audioSession.setCategory(.playAndRecord, mode: .measurement, options: .duckOthers)
            }
            
            try audioSession.setActive(true, options: .notifyOthersOnDeactivation)
            audioSessionActivated = true
         } catch {
            audioSessionActivated = false
         }
         
         guard self.isRecording, self.speechRecognizer === recognizer else {
            return
         }
         
         guard audioSessionActivated && recognizer.isAvailable else {
            self.speechRecognizer = nil
            self.isRecording = false
            
            return
         }
         
         if let previousAudioEngine = self.audioEngine {
            if previousAudioEngine.isRunning {
               previousAudioEngine.stop()
            }
            
            previousAudioEngine.inputNode.removeTap(onBus: 0)
            self.audioEngine = nil
         }
         
         let audioEngine = AVAudioEngine()
         let inputNode = audioEngine.inputNode
         let inputFormat = inputNode.outputFormat(forBus: 0)
         
         guard inputFormat.sampleRate > 0.0, inputFormat.channelCount > 0 else {
            self.speechRecognizer = nil
            
            withAnimation(.linear(duration: 0.5)) {
               self.isRecording = false
               self.volumeLevel = 0.0
            }
            
            return
         }
         
         let request = SFSpeechAudioBufferRecognitionRequest()
         
         request.shouldReportPartialResults = true
         
         self.installTap(on: inputNode, format: inputFormat, request: request) { level, duration in
            let multiplier = level > self.volumeLevel ? 5.0 : 10.0
            
            withAnimation(.linear(duration: duration * multiplier)) {
               self.volumeLevel = level
            }
         }
         
         audioEngine.prepare()
         
         do {
            try audioEngine.start()
         } catch {
            self.speechRecognizer = nil
            
            withAnimation(.linear(duration: 0.5)) {
               self.isRecording = false
               self.volumeLevel = 0.0
            }
            
            return
         }
         
         self.audioEngine = audioEngine
         self.speechAudioBufferRecognitionRequest = request
         self.speechRecognitionTask = recognizer.recognitionTask(with: request, resultHandler: { result, error in
            if error == nil {
               if let result {
                  let text = result.bestTranscription.formattedString
                  
                  if result.isFinal, self.audioEngine === audioEngine {
                     if audioEngine.isRunning {
                        audioEngine.stop()
                     }
                     
                     audioEngine.inputNode.removeTap(onBus: 0)
                     self.audioEngine = nil
                     
                     withAnimation(.linear(duration: 0.5)) {
                        self.isRecording = false
                        self.volumeLevel = 0.0
                     }
                  }
                  
                  if !text.isEmpty {
                     self.input = input + text
                  }
               }
            } else if self.audioEngine === audioEngine {
               if audioEngine.isRunning {
                  audioEngine.stop()
               }
               
               audioEngine.inputNode.removeTap(onBus: 0)
               self.audioEngine = nil
               
               withAnimation(.linear(duration: 0.5)) {
                  self.isRecording = false
                  self.volumeLevel = 0.0
               }
            }
         })
      }
   }
   
   private func stopRecognize() {
      self.speechRecognizer = nil
      
      if let speechRecognitionTask = self.speechRecognitionTask {
         speechRecognitionTask.cancel()
         self.speechRecognitionTask = nil
      }
      
      if let audioEngine = self.audioEngine {
         if audioEngine.isRunning {
            audioEngine.stop()
         }
         
         audioEngine.inputNode.removeTap(onBus: 0)
         
         self.audioEngine = nil
      }
      
      if let speechAudioBufferRecognitionRequest = self.speechAudioBufferRecognitionRequest {
         speechAudioBufferRecognitionRequest.endAudio()
         self.speechAudioBufferRecognitionRequest = nil
      }
      
      withAnimation(.linear(duration: 0.5)) {
         self.isRecording = false
         self.volumeLevel = 0.0
      }
   }
   
   private nonisolated func installTap(on inputNode: AVAudioInputNode, format: AVAudioFormat, request: SFSpeechAudioBufferRecognitionRequest, onChange: @escaping @MainActor (Double, Double) -> Void
   ) {
      inputNode.installTap(onBus: 0, bufferSize: 1024, format: format) { buffer, _ in
         guard buffer.frameLength > 0, buffer.stride > 0, buffer.format.sampleRate > 0.0 else {
            return
         }
         
         request.append(buffer)
         
         guard let floatChannelData = buffer.floatChannelData else {
            return
         }
         
         let pointee = floatChannelData.pointee
         var sum: Float = 0.0
         
         for i in stride(from: 0, to: Int(buffer.frameLength), by: buffer.stride) {
            sum += pointee[i] * pointee[i]
         }
         
         let rms = sqrt(sum / Float(buffer.frameLength))
         let dB = rms == 0.0 ? 0.0 : 20.0 * log10(rms)
         let minimum: Float = -50.0
         let maximum: Float = -25.0
         let level = Double(dB > maximum ? 1.0 : (abs(minimum) - abs(max(dB, minimum))) / (abs(minimum) - abs(maximum)))
         let duration = Double(buffer.frameLength) / buffer.format.sampleRate
         
         Task { @MainActor in
            onChange(level, duration)
         }
      }
   }
}

struct Camera: View {
   private let active: Bool
   @Binding private var text: String
   @Environment(\.dismiss) private var dismiss
   @State private var isRecognizable = true
   @State private var isPaused = false
   @State private var recognizeRegion = CGRect.zero
   @State private var recognizedText = String()
   
   var body: some View {
      NavigationStack {
         ZStack {
            Capture(recognizable: self.$isRecognizable, pause: self.isPaused, region: self.$recognizeRegion, text: self.$recognizedText)
               .frame(
                  maxWidth: .infinity,
                  maxHeight: .infinity
               )
               .background(.clear)
            Path { path in
               let radius = 16.0
               
               path.move(to: CGPoint(x: self.recognizeRegion.origin.x, y: self.recognizeRegion.origin.y + radius * 2.0))
               path.addLine(to: CGPoint(x: self.recognizeRegion.origin.x, y: self.recognizeRegion.origin.y + radius))
               path.addQuadCurve(to: CGPoint(x: self.recognizeRegion.origin.x + radius, y: self.recognizeRegion.origin.y), control: self.recognizeRegion.origin)
               path.addLine(to: CGPoint(x: self.recognizeRegion.origin.x + radius * 2.0, y: self.recognizeRegion.origin.y))
               path.move(to: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width - radius * 2.0, y: self.recognizeRegion.origin.y))
               path.addLine(to: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width - radius, y: self.recognizeRegion.origin.y))
               path.addQuadCurve(to: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width, y: self.recognizeRegion.origin.y + radius), control: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width, y: self.recognizeRegion.origin.y))
               path.addLine(to: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width, y: self.recognizeRegion.origin.y + radius * 2.0))
               path.move(to: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width, y: self.recognizeRegion.origin.y + self.recognizeRegion.height - radius * 2.0))
               path.addLine(to: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width, y: self.recognizeRegion.origin.y + self.recognizeRegion.height - radius))
               path.addQuadCurve(to: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width - radius, y: self.recognizeRegion.origin.y + self.recognizeRegion.height), control: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width, y: self.recognizeRegion.origin.y + self.recognizeRegion.height))
               path.addLine(to: CGPoint(x: self.recognizeRegion.origin.x + self.recognizeRegion.width - radius * 2.0, y: self.recognizeRegion.origin.y + self.recognizeRegion.height))
               path.move(to: CGPoint(x: self.recognizeRegion.origin.x + radius * 2.0, y: self.recognizeRegion.origin.y + self.recognizeRegion.height))
               path.addLine(to: CGPoint(x: self.recognizeRegion.origin.x + radius, y: self.recognizeRegion.origin.y + self.recognizeRegion.height))
               path.addQuadCurve(to: CGPoint(x: self.recognizeRegion.origin.x, y: self.recognizeRegion.origin.y + self.recognizeRegion.height - radius), control: CGPoint(x: self.recognizeRegion.origin.x, y: self.recognizeRegion.origin.y + self.recognizeRegion.height))
               path.addLine(to: CGPoint(x: self.recognizeRegion.origin.x, y: self.recognizeRegion.origin.y + self.recognizeRegion.height - radius * 2.0))
            }
            .stroke(Color(hue: 0.0, saturation: 0.0, brightness: 1.0, opacity: 1.0), lineWidth: 2.0)
            
            if !self.isRecognizable {
               Image(systemName: "exclamationmark.triangle")
                  .symbolRenderingMode(.monochrome)
                  .frame(
                     width: 16.0,
                     height: 16.0,
                     alignment: .center
                  )
                  .background(.clear)
                  .foregroundStyle(Color(hue: 0.0, saturation: 0.0, brightness: 1.0, opacity: 1.0))
                  .font(
                     .system(size: 16.0)
                  )
                  .bold()
            } else if self.isPaused {
               Image(systemName: "pause")
                  .frame(
                     width: 16.0,
                     height: 16.0,
                     alignment: .center
                  )
                  .background(.clear)
                  .foregroundStyle(Color(hue: 0.0, saturation: 0.0, brightness: 1.0, opacity: 1.0))
                  .font(.system(size: 16.0))
                  .bold()
            } else {
               Button(action: {
                  self.text = self.recognizedText
                  
                  dismiss()
               }) {
                  ZStack {
                     Prompt(active: self.active, input: (self.recognizedText, nil, false, nil, [], 0, CACurrentMediaTime()), accent: UIColor(white: 1.0, alpha: 1.0), font: UIFont.systemFont(ofSize: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0), weight: .semibold))
                        .frame(
                           height: ceil(UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).lineHeight),
                           alignment: .center
                        )
                        .offset(y: ceil(UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).leading / 2.0))
                        .padding(0.0)
                        .background(.clear)
                  }
                  .frame(
                     maxWidth: .infinity,
                     maxHeight: .infinity
                  )
                  .padding(EdgeInsets(
                     top: 8.0,
                     leading: 16.0,
                     bottom: 8.0,
                     trailing: 16.0
                  ))
                  .background(.clear)
               }
               .frame(
                  width: self.recognizeRegion.width,
                  height: ceil(UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).lineHeight - UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).leading),
                  alignment: .center
               )
               .background(.clear)
            }
         }
         .frame(
            maxWidth: .infinity,
            maxHeight: .infinity
         )
         .ignoresSafeArea(.all)
         .background(Color(hue: 0.0, saturation: 0.0, brightness: 0.0, opacity: 1.0))
         .navigationBarTitleDisplayMode(.inline)
         .toolbarBackground(.hidden, for: .navigationBar)
         .toolbar {
            ToolbarItem(placement: .cancellationAction) {
               Button(action: {
                  dismiss()
               }) {
                  ZStack {
                     Image(systemName: "xmark")
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .foregroundStyle(.primary)
                        .font(.system(size: 8.0))
                        .bold()
                  }
               }
               .contentShape(Circle())
            }
         }
         .transition(.opacity)
         .onLongPressGesture(perform: {
            withAnimation(.linear(duration: 0.5)) {
               self.isPaused.toggle()
            }
         })
      }
   }
   
   init(active: Bool, text: Binding<String>) {
      self.active = active
      self._text = text
   }
}

struct Capture: UIViewControllerRepresentable {
   @Binding private var recognizable: Bool
   private var pause: Bool
   @Binding private var region: CGRect
   @Binding private var text: String
   
   init(recognizable: Binding<Bool>, pause: Bool, region: Binding<CGRect>, text: Binding<String>) {
      self._recognizable = recognizable
      self.pause = pause
      self._region = region
      self._text = text
   }
   
   func makeUIViewController(context: Context) -> CaptureViewController {
      let viewController = CaptureViewController()
      
      viewController.delegate = context.coordinator
      
      return viewController
   }
   
   func updateUIViewController(_ uiViewController: CaptureViewController, context: Context) {
      if self.pause != uiViewController.isPaused {
         uiViewController.isPaused = self.pause
      }
      
      context.coordinator.recognizable = self.$recognizable
      context.coordinator.region = self.$region
      context.coordinator.text = self.$text
   }
   
   func makeCoordinator() -> Coordinator {
      return Coordinator(recognizable: self.$recognizable, region: self.$region, text: self.$text)
   }
   
   protocol CaptureDelegate: AnyObject {
      @MainActor
      func captureDidUpdate(_ capture: CaptureViewController)
      @MainActor
      func captureDidFail(_ capture: CaptureViewController)
   }
   
   class Coordinator: NSObject, CaptureDelegate {
      var recognizable: Binding<Bool>
      var region: Binding<CGRect>
      var text: Binding<String>
      
      init(recognizable: Binding<Bool>, region: Binding<CGRect>, text: Binding<String>) {
         self.recognizable = recognizable
         self.region = region
         self.text = text
      }
      
      @MainActor
      func captureDidUpdate(_ capture: Capture.CaptureViewController) {
         self.region.wrappedValue = capture.recognizeRegion
         self.text.wrappedValue = capture.recognizedText
      }
      
      @MainActor
      func captureDidFail(_ capture: Capture.CaptureViewController) {
         self.recognizable.wrappedValue = capture.isRecognizable
      }
   }
   
   class CaptureViewController: UIViewController, AVCaptureVideoDataOutputSampleBufferDelegate {
      var delegate: CaptureDelegate? = nil
      var isRecognizable = true
      var isPaused = false {
         didSet {
            if self.isPaused != oldValue {
               self.frameState.withLock { state in
                  state.generation &+= 1
               }
            }
         }
      }
      var recognizedText = String()
      var recognizeRegion = CGRect.zero
      private let sessionQueue = DispatchQueue(label: String(describing: Capture.CaptureViewController.self))
      private let captureSession = Mutex(AVCaptureSession())
      private let frameState = Mutex((isProcessing: false, generation: UInt64(0)))
      private var captureVideoPreviewLayer: AVCaptureVideoPreviewLayer? = nil
      private var recognitionLanguage: String? = nil
      private var isCaptureSessionConfigured = false
      private var isConfiguringCaptureSession = false
      private var isViewVisible = false
      private var elapsedTime = 0.0

      override func viewDidLoad() {
         super.viewDidLoad()
         
         let recognizeTextRequest = VNRecognizeTextRequest()
         
         recognizeTextRequest.preferBackgroundProcessing = true
         recognizeTextRequest.usesLanguageCorrection = true
         recognizeTextRequest.recognitionLevel = .accurate
         
         self.recognizeRegion = self.createRecognizeRegion(size: self.view.bounds.size)
         
         if let preferredLanguage = Locale.preferredLanguages.first, let languageCode = Locale(identifier: preferredLanguage).language.languageCode, let languages = try? recognizeTextRequest.supportedRecognitionLanguages(), let language = languages.first(where: { Locale(identifier: $0).language.languageCode == languageCode }) {
            self.recognitionLanguage = language
         } else {
            self.isRecognizable = false
            self.delegate?.captureDidFail(self)
            
            return
         }

         switch AVCaptureDevice.authorizationStatus(for: .video) {
         case .authorized:
            self.prepareCaptureSession()
            
         case .notDetermined:
            AVCaptureDevice.requestAccess(for: .video, completionHandler: { [weak self] granted in
               Task { @MainActor in
                  guard let self else {
                     return
                  }
                  
                  if granted {
                     self.prepareCaptureSession()
                  } else {
                     self.isRecognizable = false
                     self.delegate?.captureDidFail(self)
                  }
               }
            })
            
         default:
            self.isRecognizable = false
            self.delegate?.captureDidFail(self)
         }
      }

      private func prepareCaptureSession() {
         guard self.isRecognizable && !self.isCaptureSessionConfigured && !self.isConfiguringCaptureSession else {
            return
         }

         self.isConfiguringCaptureSession = true

         if self.captureVideoPreviewLayer == nil {
            self.captureVideoPreviewLayer = self.captureSession.withLock { session in
               AVCaptureVideoPreviewLayer(session: session)
            }
            self.captureVideoPreviewLayer!.videoGravity = AVLayerVideoGravity.resizeAspectFill
            self.captureVideoPreviewLayer!.frame = self.view.bounds
            
            self.view.layer.addSublayer(self.captureVideoPreviewLayer!)
         }

         self.sessionQueue.async { [weak self] in
            guard let self else {
               return
            }

            let configured = self.configureCaptureSession()

            Task { @MainActor [weak self] in
               guard let self else {
                  return
               }

               self.isConfiguringCaptureSession = false

               if configured {
                  self.isCaptureSessionConfigured = true

                  if self.isViewVisible {
                     self.startCaptureSession()
                  }
               } else {
                  self.isCaptureSessionConfigured = false
                  self.isRecognizable = false
                  self.delegate?.captureDidFail(self)
               }
            }
         }
      }

      private func startCaptureSession() {
         guard self.isRecognizable && self.isCaptureSessionConfigured && self.isViewVisible else {
            return
         }

         self.sessionQueue.async { [weak self] in
            guard let self else {
               return
            }

            self.captureSession.withLock { session in
               if !session.isRunning {
                  session.startRunning()
               }
            }

            Task { @MainActor [weak self] in
               guard let self, let captureVideoPreviewLayer = self.captureVideoPreviewLayer else {
                  return
               }
               
               let angle: CGFloat
               
               switch self.view.window?.windowScene?.effectiveGeometry.interfaceOrientation ?? .portrait {
               case .portraitUpsideDown:
                  angle = 270
               case .landscapeLeft:
                  angle = 180
               case .landscapeRight:
                  angle = 0
               default:
                  angle = 90
               }
               
               if let connection = captureVideoPreviewLayer.connection, connection.isVideoRotationAngleSupported(angle) {
                  connection.videoRotationAngle = angle
               }
            }
         }
      }

      private func stopCaptureSession() {
         guard self.isCaptureSessionConfigured else {
            return
         }

         self.sessionQueue.async { [weak self] in
            self?.captureSession.withLock { session in
               if session.isRunning {
                  session.stopRunning()
               }
            }
         }
      }

      nonisolated private func configureCaptureSession() -> Bool {
         return self.captureSession.withLock { session in
            guard let captureDevice = AVCaptureDevice.default(for: .video), let input = try? AVCaptureDeviceInput(device: captureDevice) else {
               return false
            }

            let output = AVCaptureVideoDataOutput()

            output.videoSettings = [kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32BGRA]
            output.setSampleBufferDelegate(self, queue: self.sessionQueue)
            output.alwaysDiscardsLateVideoFrames = true

            guard session.canAddInput(input) else {
               return false
            }

            session.beginConfiguration()
            
            defer {
               session.commitConfiguration()
            }

            session.addInput(input)

            guard session.canAddOutput(output) else {
               session.removeInput(input)

               return false
            }

            session.addOutput(output)

            if let connection = output.connection(with: .video), connection.isVideoStabilizationSupported {
               connection.preferredVideoStabilizationMode = .auto
            }

            if session.canSetSessionPreset(.photo) {
               session.sessionPreset = .photo
            }

            return true
         }
      }
      
      override func viewDidLayoutSubviews() {
         super.viewDidLayoutSubviews()

         var layoutChanged = false
         let recognizeRegion = self.createRecognizeRegion(size: self.view.bounds.size)

         if self.recognizeRegion != recognizeRegion {
            self.recognizeRegion = recognizeRegion
            layoutChanged = true
         }

         if let captureVideoPreviewLayer = self.captureVideoPreviewLayer {
            let angle: CGFloat
            
            if captureVideoPreviewLayer.bounds.size != self.view.bounds.size {
               layoutChanged = true
            }

            captureVideoPreviewLayer.frame = self.view.bounds
            
            switch self.view.window?.windowScene?.effectiveGeometry.interfaceOrientation ?? .portrait {
            case .portraitUpsideDown:
               angle = 270
            case .landscapeLeft:
               angle = 180
            case .landscapeRight:
               angle = 0
            default:
               angle = 90
            }
            
            if let connection = captureVideoPreviewLayer.connection, connection.isVideoRotationAngleSupported(angle) {
               if connection.videoRotationAngle != angle {
                  layoutChanged = true
               }

               connection.videoRotationAngle = angle
            }
         }

         if layoutChanged {
            self.frameState.withLock { state in
               state.generation &+= 1
            }
            self.delegate?.captureDidUpdate(self)
         }
      }
      
      override func viewWillAppear(_ animated: Bool) {
         super.viewWillAppear(animated)
         
         self.frameState.withLock { state in
            state.generation &+= 1
         }
         self.isViewVisible = true
         self.startCaptureSession()
      }
      
      override func viewWillDisappear(_ animated: Bool) {
         self.isViewVisible = false
         self.frameState.withLock { state in
            state.generation &+= 1
         }
         self.stopCaptureSession()
         
         super.viewWillDisappear(animated)
      }
      
      nonisolated func captureOutput(_ output: AVCaptureOutput, didOutput sampleBuffer: CMSampleBuffer, from connection: AVCaptureConnection) {
         guard let pixelBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) else {
            return
         }

         let generation = self.frameState.withLock { state -> UInt64? in
            guard !state.isProcessing else {
               return nil
            }

            state.isProcessing = true

            return state.generation
         }

         guard let generation else {
            return
         }

         let image = CIImage(cvImageBuffer: pixelBuffer)

         Task { @MainActor [weak self] in
            guard let self else {
               return
            }

            defer {
               self.frameState.withLock { state in
                  state.isProcessing = false
               }
            }

            let isCurrent = self.frameState.withLock { state in
               state.generation == generation
            }

            guard isCurrent else {
               return
            }

            let orientation: CGImagePropertyOrientation

            switch self.view.window?.windowScene?.effectiveGeometry.interfaceOrientation ?? .portrait {
            case .portraitUpsideDown:
               orientation = .left
            case .landscapeLeft:
               orientation = .down
            case .landscapeRight:
               orientation = .up
            default:
               orientation = .right
            }

            let currentMediaTime = CACurrentMediaTime()

            if self.isViewVisible && !self.isPaused && currentMediaTime - self.elapsedTime >= 1.0, let captureVideoPreviewLayer = self.captureVideoPreviewLayer, let recognitionLanguage = self.recognitionLanguage {
               let metadataRegion = captureVideoPreviewLayer.metadataOutputRectConverted(fromLayerRect: self.recognizeRegion)
               let recognizeRegion = CGRect(x: metadataRegion.minX, y: 1.0 - metadataRegion.maxY, width: metadataRegion.width, height: metadataRegion.height).intersection(CGRect(x: 0.0, y: 0.0, width: 1.0, height: 1.0))

               guard !recognizeRegion.isNull && recognizeRegion.width > 0.0 && recognizeRegion.height > 0.0 else {
                  return
               }

               self.elapsedTime = currentMediaTime

               let recognizedText = await Task.detached { @Sendable [image, recognitionLanguage, recognizeRegion, orientation] in
                  let recognizeTextRequest = VNRecognizeTextRequest()

                  recognizeTextRequest.preferBackgroundProcessing = true
                  recognizeTextRequest.usesLanguageCorrection = true
                  recognizeTextRequest.recognitionLevel = .accurate
                  recognizeTextRequest.recognitionLanguages = [recognitionLanguage]
                  recognizeTextRequest.regionOfInterest = recognizeRegion

                  try? VNImageRequestHandler(ciImage: image, orientation: orientation, options: [:]).perform([recognizeTextRequest])

                  var maxConfidence: VNConfidence = 0.0
                  var text: String? = nil

                  for observation in recognizeTextRequest.results ?? [] {
                     if let first = observation.topCandidates(1).first, first.confidence > maxConfidence {
                        text = first.string
                        maxConfidence = first.confidence
                     }
                  }

                  guard let text, maxConfidence >= 0.5 else {
                     return String()
                  }

                  return text.replacingOccurrences(of: "\n", with: String()).trimmingCharacters(in: .whitespaces)
               }.value

               let isCurrent = self.frameState.withLock { state in
                  state.generation == generation
               }

               guard isCurrent && self.isViewVisible && !self.isPaused else {
                  return
               }

               self.recognizedText = recognizedText
               self.delegate?.captureDidUpdate(self)
            }
         }
      }
      
      private func createRecognizeRegion(size: CGSize) -> CGRect {
         let length = min(size.width, size.height) * 0.75
         
         return CGRect(origin: CGPoint(x: (size.width - length) / 2.0, y: (size.height - length) / 2.0), size: CGSize(width: length, height: length))
      }
   }
}

struct Gallery: View {
   let accent: UIColor
   @Environment(\.dismiss) private var dismiss
   @State private var page = 0
   @State private var position = 1
   @State private var paths = [String]()
   @State private var playables = [(Bool, Bool)]()
   
   var body: some View {
      NavigationStack {
         VStack(spacing: 0.0) {
            Group {
               if self.paths.indices.contains(self.page) && self.paths.count == self.playables.count {
                  TabView(selection: self.$page) {
                     ForEach(Array(self.paths.enumerated()), id: \.offset) { (index, path) in
                        ZStack {
                           ZStack {
                              if self.playables[index].1 {
                                 Player(path: path, accent: self.accent)
                                    .frame(
                                       maxWidth: .infinity,
                                       maxHeight: .infinity
                                    )
                                    .background(.clear)
                              }
                           }
                           .frame(maxWidth: .infinity, maxHeight: .infinity)
                           .background(Color(uiColor: .systemBackground))
                           .clipShape(RoundedRectangle(cornerRadius: 16.0))
                           .shadow(color: Color(hue: 0.0, saturation: 0.0, brightness: 0.0, opacity: 0.25), radius: 8.0, x: 0.0, y: 0.0)
                           .opacity(self.playables[index].0 ? 1.0 : 0.0)
                           .transaction {
                              $0.addAnimationCompletion(criteria: .logicallyComplete) {
                                 if self.playables.indices.contains(index) && !self.playables[index].0 && self.playables[index].1 {
                                    self.playables[index] = (false, false)
                                 }
                              }
                           }
                           .animation(.easeInOut(duration: 0.5), value: self.playables[index].0)
                        }
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                        .background(.clear)
                        .padding(16.0)
                        .tag(index)
                        .transition(.opacity.animation(.linear))
                        .onAppear {
                           guard self.paths.indices.contains(index) else {
                              return
                           }
                           
                           self.playables[index] = (true, true)
                        }
                        .onDisappear {
                           guard self.paths.indices.contains(index) else {
                              return
                           }

                           self.playables[index] = (false, false)
                        }
                     }
                  }
                  .tabViewStyle(.page(indexDisplayMode: .never))
               } else {
                  Color.clear
               }
            }
            .frame(
               maxWidth: .infinity,
               maxHeight: .infinity
            )
            .background(.clear)
            ZStack {
               HStack(alignment: .center, spacing: 16.0) {
                  Text(String(format: "%ld", self.position))
                     .background(.clear)
                     .foregroundStyle(.primary)
                     .font(.custom("DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .footnote).pointSize)))
                     .lineLimit(1)
                     .truncationMode(.tail)
                     .contentTransition(.numericText(value: Double(self.position)))
                  Text(String(format: "%ld", max(self.paths.count, 1)))
                     .background(.clear)
                     .foregroundStyle(.primary)
                     .font(.custom("DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .footnote).pointSize)))
                     .lineLimit(1)
                     .truncationMode(.tail)
                     .contentTransition(.numericText(value: Double(max(self.paths.count, 1))))
               }
               Rectangle()
                  .fill(.primary)
                  .frame(
                     width: 1.0,
                     height: 16.0
                  )
            }
            .padding(0.0)
            .opacity(self.paths.isEmpty ? 0.0 : 1.0)
            .animation(.linear(duration: 0.5), value: self.paths.isEmpty)
         }
         .frame(
            maxWidth: .infinity,
            maxHeight: .infinity
         )
         .background(.clear)
         .scrollContentBackground(.hidden)
         .navigationBarTitleDisplayMode(.inline)
         .toolbarBackground(.hidden, for: .navigationBar)
         .toolbar {
            ToolbarItem(placement: .principal) {
               Text("Gallery")
                  .foregroundStyle(.primary)
                  .font(.headline)
                  .fontWeight(.semibold)
                  .lineLimit(1)
                  .textCase(.uppercase)
            }
            ToolbarItem(placement: .cancellationAction) {
               Button(action: {
                  dismiss()
               }) {
                  ZStack {
                     Image(systemName: "xmark")
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .foregroundStyle(.primary)
                        .font(
                           .system(size: 8.0)
                        )
                        .bold()
                  }
               }
               .contentShape(Circle())
            }
            ToolbarItem(placement: .primaryAction) {
               Button(action: {
                  guard self.paths.indices.contains(self.page), self.playables.indices.contains(self.page) else {
                     return
                  }

                  let removedPage = self.page
                  let path = self.paths[removedPage]
                  let remainingCount = self.paths.count - 1
                  let nextPage = remainingCount > 0 ? min(removedPage, remainingCount - 1) : 0

                  withAnimation {
                     self.page = nextPage
                     self.paths.remove(at: removedPage)
                     self.playables.remove(at: removedPage)

                     if self.playables.indices.contains(self.page) {
                        self.playables[self.page] = (true, true)
                     }
                  }

                  Task.detached {
                     if FileManager.default.fileExists(atPath: path) {
                        try? FileManager.default.removeItem(atPath: path)
                     }
                  }
               }) {
                  ZStack {
                     Image(systemName: "trash")
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .foregroundStyle(.primary)
                        .font(
                           .system(size: 8.0)
                        )
                        .bold()
                        .opacity(self.paths.indices.contains(self.page) ? 1.0 : 0.5)
                        .animation(.linear, value: self.paths.indices.contains(self.page))
                  }
               }
               .contentShape(Circle())
               .disabled(!self.paths.indices.contains(self.page))
            }
         }
         .transition(.opacity)
         .onChange(of: self.page) {
            withAnimation {
               self.position = self.page + 1
            }
         }
         .task {
            let paths = await self.load()
            
            withAnimation {
               for path in paths {
                  self.paths.append(path)
                  self.playables.append((false, false))
               }
            }
         }
      }
   }
   
   private func load() async -> [String] {
      return await Task.detached {
         var imagePaths = [String]()
         
         if let documentUrl = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first, let urls = try? FileManager.default.contentsOfDirectory(at: documentUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
            let regex = /(?i)([0-9A-HJKMNP-TV-Z]{26})(?=\.png$)/
            
            for url in urls {
               if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), let isDirectory = values.isDirectory, !isDirectory, let name = values.name, name.firstMatch(of: regex) != nil {
                  imagePaths.append(documentUrl.appending(path: name, directoryHint: .inferFromPath).path(percentEncoded: false))
               }
            }
         }
         
         imagePaths.sort { $0 > $1 }
         
         return imagePaths
      }.value
   }
}

struct Player: UIViewRepresentable {
   let path: String
   let accent: UIColor
   
   func makeUIView(context: Context) -> PlayerView {
      let playerView = PlayerView(frame: .zero)
      
      playerView.change(accent: self.accent.cgColor)
      
      return playerView
   }
   
   func updateUIView(_ uiView: PlayerView, context: Context) {
      uiView.accentColor = self.accent.cgColor
      
      if context.coordinator.path != self.path {
         context.coordinator.path = self.path
         
         Task {
            await uiView.fetch(path: self.path)
         }
      }
   }
   
   func makeCoordinator() -> Coordinator {
      return Coordinator()
   }
   
   class Coordinator: NSObject {
      var path: String? = nil
   }
   
   class PlayerView: UIView {
      private var displayLink: CADisplayLink? = nil
      private var isReloading = false
      private var isLoading = false
      private var isFetched = false
      private var revealStep: Double = -1.0
      private var loadingStep: Double = -1.0
      private var fetchedFrames: [(image: CGImage?, delay: Double)]? = nil
      private var backgroundFrames: [(image: CGImage?, delay: Double)]? = nil
      private var blindColor: CGColor? = nil
      private var currentTime: Double? = nil
      private var loadingLayer: CALayer? = nil
      private var blindLayer: CALayer? = nil
      private let backgroundPattern = UIImage(named: "Stripes")!
      var accentColor: CGColor? {
         get {
            return self.blindColor
         }
         set(color) {
            self.blindColor = color
         }
      }
      
      override init(frame: CGRect) {
         super.init(frame: frame)
         
         let loadingLayer = CALayer()
         let blindLayer = CALayer()
         
         loadingLayer.backgroundColor = UIColor(patternImage: self.backgroundPattern).cgColor
         loadingLayer.contentsGravity = .topLeft
         loadingLayer.opacity = 0.125
         loadingLayer.transform = CATransform3DMakeScale(1.0, -1.0, 1.0)
         
         var red: CGFloat = 0
         var green: CGFloat = 0
         var blue: CGFloat = 0
         
         UIColor(named: "AccentColor")!.getRed(&red, green: &green, blue: &blue, alpha: nil)
         
         blindLayer.backgroundColor = CGColor(colorSpace: CGColorSpace(name: CGColorSpace.displayP3) ?? CGColorSpaceCreateDeviceRGB(), components: [red, green, blue, 1.0])!
         blindLayer.contentsGravity = .resizeAspect
         blindLayer.masksToBounds = true
         
         let length = max(self.bounds.size.width, self.bounds.size.height)
         
         blindLayer.frame = CGRect(x: 0.0, y: 0.0, width: length, height: length)
         loadingLayer.frame = CGRect(x: 0.0, y: 0.0, width: length + self.backgroundPattern.size.width, height: length)
         
         blindLayer.addSublayer(loadingLayer)
         
         self.blindColor = blindLayer.backgroundColor
         self.isUserInteractionEnabled = true
         self.isMultipleTouchEnabled = true
         self.backgroundColor = .clear
         self.contentMode = .center
         self.loadingLayer = loadingLayer
         self.blindLayer = blindLayer
         self.layer.contentsGravity = .resizeAspect
         self.layer.addSublayer(self.blindLayer!)
      }
      
      required init?(coder aDecoder: NSCoder) {
         super.init(coder: aDecoder)
      }
      
      override func layoutSubviews() {
         super.layoutSubviews()
         
         guard self.isLoading, let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer else {
            return
         }
         
         let length = max(self.bounds.size.width, self.bounds.size.height)
         
         guard length > 0.0 else {
            return
         }
         
         let revealStep = min(max(self.revealStep, -1.0), 1.0)
         let loadingStep = max(self.loadingStep, 0.0)
         
         CATransaction.begin()
         CATransaction.setDisableActions(true)
         
         blindLayer.frame = CGRect(x: 0.0, y: -length * sin(revealStep / 2.0 * Double.pi), width: length, height: length)
         loadingLayer.frame = CGRect(x: -self.backgroundPattern.size.width * loadingStep, y: 0.0, width: length + self.backgroundPattern.size.width, height: length)
         
         CATransaction.commit()
      }
      
      func change(accent: CGColor) {
         self.blindColor = accent
         self.blindLayer?.backgroundColor = accent
      }
      
      func fetch(path: String) async {
         self.isReloading = true
         self.isLoading = true
         self.isFetched = false
         
         Task {
            let length = max(self.bounds.size.width, self.bounds.size.height) * self.traitCollection.displayScale
            
            self.fetchedFrames = await Task.detached {
               var frames = [(image: CGImage?, delay: Double)]()
               
               if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                  defer {
                     try? file.close()
                  }
                  
                  if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                     for i in 0..<CGImageSourceGetCount(imageSource) {
                        if let image = CGImageSourceCreateImageAtIndex(imageSource, i, nil), let properties = CGImageSourceCopyPropertiesAtIndex(imageSource, i, nil) as? [String: Any] {
                           let width: Double
                           let height: Double
                           var resizedImage: CGImage? = nil
                           var delay = 0.0
                           
                           if image.width < image.height {
                              if length > 0.0 && Double(image.width) > length {
                                 width = length
                                 height = floor(length / Double(image.width) * Double(image.height))
                              } else {
                                 width = Double(image.width)
                                 height = Double(image.height)
                              }
                           } else if length > 0.0 && Double(image.height) > length {
                              width = floor(length / Double(image.height) * Double(image.width))
                              height = length
                           } else {
                              width = Double(image.width)
                              height = Double(image.height)
                           }
                           
                           let size = CGSize(width: width, height: height)
                           let rendererFormat = UIGraphicsImageRendererFormat()
                           
                           rendererFormat.opaque = false
                           rendererFormat.scale = 1.0
                           rendererFormat.preferredRange = .standard
                           
                           let renderer = UIGraphicsImageRenderer(size: size, format: rendererFormat)
                           
                           resizedImage = renderer.image { rendererContext in
                              let context = rendererContext.cgContext
                              
                              context.interpolationQuality = .high
                              context.setAllowsAntialiasing(true)
                              context.clear(CGRect(x: 0.0, y: 0.0, width: width, height: height))
                              context.translateBy(x: 0.0, y: height)
                              context.scaleBy(x: 1.0, y: -1.0)
                              context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: width, height: height))
                           }.cgImage
                           
                           for (key, value) in properties {
                              if key == kCGImagePropertyPNGDictionary as String, let dictionary = value as? [String: Any] {
                                 if let delayTime = dictionary[kCGImagePropertyAPNGUnclampedDelayTime as String] {
                                    if let number = delayTime as? NSNumber {
                                       let doubleValue = number.doubleValue
                                       
                                       if doubleValue <= 0.01 {
                                          delay = 0.1
                                       } else {
                                          delay = doubleValue
                                       }
                                    }
                                 } else if let delayTime = dictionary[kCGImagePropertyAPNGDelayTime as String] {
                                    if let number = delayTime as? NSNumber {
                                       let doubleValue = number.doubleValue
                                       
                                       if doubleValue <= 0.01 {
                                          delay = 0.1
                                       } else {
                                          delay = doubleValue
                                       }
                                    }
                                 }
                              }
                           }
                           
                           frames.append((image: resizedImage, delay: delay))
                        }
                     }
                     
                     for i in stride(from: frames.count - 2, through: 0, by: -1) {
                        frames.append(frames[i])
                     }
                     
                     return frames
                  }
               }
               
               return nil
            }.value
            
            self.isReloading = false
         }
      }
      
      override func didMoveToWindow() {
         super.didMoveToWindow()
         
         if self.window == nil {
            self.displayLink?.invalidate()
            self.displayLink = nil
         } else if self.displayLink == nil {
            let displayLink = CADisplayLink(target: self, selector: #selector(self.step))
            
            self.displayLink = displayLink
            displayLink.add(to: .current, forMode: .common)
         }
      }
      
      @objc private func step(displayLink: CADisplayLink) {
         if self.bounds.size.width > 0 && self.bounds.size.height > 0 {
            let deltaTime = displayLink.targetTimestamp - displayLink.timestamp
            
            if self.isLoading {
               if self.isFetched {
                  let step = self.revealStep + deltaTime
                  
                  if step >= 1.0 {
                     if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer {
                        CATransaction.begin()
                        CATransaction.setDisableActions(true)
                        
                        blindLayer.frame = CGRect.zero
                        loadingLayer.frame = CGRect.zero
                        
                        CATransaction.commit()
                     }
                     
                     self.revealStep = -1.0
                     self.loadingStep = 0.0
                     self.isLoading = false
                  } else {
                     if let blindLayer = self.blindLayer {
                        let length = max(self.bounds.size.width, self.bounds.size.height)
                        
                        CATransaction.begin()
                        CATransaction.setDisableActions(true)
                        
                        blindLayer.frame = CGRect(x: 0.0, y: -length * sin(step / 2.0 * Double.pi), width: length, height: length)
                        
                        CATransaction.commit()
                     }
                     
                     self.loadingStep += deltaTime
                     
                     if self.loadingStep >= 1.0 {
                        self.loadingStep = 0.0
                     }
                     
                     if let loadingLayer = self.loadingLayer {
                        CATransaction.begin()
                        CATransaction.setDisableActions(true)
                        
                        loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                        
                        CATransaction.commit()
                     }
                     
                     self.revealStep = step
                  }
               } else if self.revealStep > 0.0 {
                  let step = self.revealStep - deltaTime
                  
                  self.loadingStep += deltaTime
                  
                  if self.loadingStep >= 1.0 {
                     self.loadingStep = 0.0
                  }
                  
                  if step <= 0.0 {
                     if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer {
                        let length = max(self.bounds.size.width, self.bounds.size.height)
                        
                        CATransaction.begin()
                        CATransaction.setDisableActions(true)
                        
                        blindLayer.frame = CGRect(x: 0.0, y: 0.0, width: length, height: length)
                        loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                        
                        CATransaction.commit()
                     }
                     
                     self.revealStep = 0.0
                     
                     if !self.isReloading {
                        self.backgroundFrames = self.fetchedFrames
                        self.currentTime = nil
                        self.isFetched = true
                     }
                  } else {
                     if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer {
                        let length = max(self.bounds.size.width, self.bounds.size.height)
                        
                        CATransaction.begin()
                        CATransaction.setDisableActions(true)
                        
                        blindLayer.frame = CGRect(x: 0.0, y: -length * sin(step / 2.0 * Double.pi), width: length, height: length)
                        loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                        
                        CATransaction.commit()
                     }
                     
                     self.revealStep = step
                  }
               } else {
                  let step = self.revealStep + deltaTime
                  
                  if step >= 0.0 {
                     if self.loadingStep < 0.0 {
                        self.loadingStep = 0.0
                        
                        if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer {
                           let length = max(self.bounds.size.width, self.bounds.size.height)
                           
                           CATransaction.begin()
                           CATransaction.setDisableActions(true)
                           
                           blindLayer.frame = CGRect(x: 0.0, y: 0.0, width: length, height: length)
                           loadingLayer.frame = CGRect(x: 0.0, y: 0.0, width: length + self.backgroundPattern.size.width, height: length)
                           
                           CATransaction.commit()
                        }
                     } else {
                        self.loadingStep += deltaTime
                        
                        if self.loadingStep >= 1.0 {
                           self.loadingStep = 0.0
                        }
                        
                        if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer {
                           let length = max(self.bounds.size.width, self.bounds.size.height)
                           
                           CATransaction.begin()
                           CATransaction.setDisableActions(true)
                           
                           blindLayer.frame = CGRect(x: 0.0, y: 0.0, width: length, height: length)
                           loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                           
                           CATransaction.commit()
                        }
                     }
                     
                     self.revealStep = 0.0
                     
                     if !self.isReloading {
                        self.backgroundFrames = self.fetchedFrames
                        self.currentTime = nil
                        self.isFetched = true
                     }
                  } else {
                     if self.revealStep > -1.0 {
                        if let blindLayer = self.blindLayer {
                           let length = max(self.bounds.size.width, self.bounds.size.height)
                           
                           CATransaction.begin()
                           CATransaction.setDisableActions(true)
                           
                           blindLayer.frame = CGRect(x: 0.0, y: -length * sin(step / 2.0 * Double.pi), width: length, height: length)
                           
                           CATransaction.commit()
                        }
                        
                        self.loadingStep += deltaTime
                        
                        if self.loadingStep >= 1.0 {
                           self.loadingStep = 0.0
                        }
                        
                        if let loadingLayer = self.loadingLayer {
                           CATransaction.begin()
                           CATransaction.setDisableActions(true)
                           
                           loadingLayer.frame = CGRect(x: (loadingLayer.frame.height - loadingLayer.frame.width) * self.loadingStep, y: 0.0, width: loadingLayer.frame.width, height: loadingLayer.frame.height)
                           
                           CATransaction.commit()
                        }
                     } else if let blindLayer = self.blindLayer, let loadingLayer = self.loadingLayer {
                        let length = max(self.bounds.size.width, self.bounds.size.height)
                        
                        CATransaction.begin()
                        CATransaction.setDisableActions(true)
                        
                        blindLayer.frame = CGRect(x: 0.0, y: -length * sin(step / 2.0 * Double.pi), width: length, height: length)
                        blindLayer.backgroundColor = self.blindColor
                        loadingLayer.frame = CGRect(x: 0.0, y: 0.0, width: length + self.backgroundPattern.size.width, height: length)
                        
                        CATransaction.commit()
                     }
                     
                     self.revealStep = step
                  }
               }
            }
            
            if let frames = self.backgroundFrames {
               var image: CGImage? = nil
               var updateRequired = false
               
               if let currentTime = self.currentTime {
                  if frames.count > 1 {
                     let nextTime = currentTime + deltaTime
                     var previousImage: CGImage? = nil
                     var nextImage: CGImage? = nil
                     var delay = 0.0
                     
                     for frame in frames {
                        let nextDelay = delay + frame.delay
                        
                        if currentTime >= delay && currentTime < nextDelay {
                           previousImage = frame.image
                        }
                        
                        if nextTime >= delay && nextTime < nextDelay {
                           nextImage = frame.image
                        }
                        
                        delay = nextDelay
                     }
                     
                     if nextImage == nil {
                        if previousImage != frames[0].image {
                           updateRequired = true
                        }
                        
                        image = frames[0].image
                        self.currentTime = 0.0
                     } else if previousImage == nextImage {
                        image = nextImage
                        self.currentTime = nextTime
                     } else {
                        updateRequired = true
                        image = nextImage
                        self.currentTime = nextTime
                     }
                  } else {
                     image = frames[0].image
                  }
               } else {
                  image = frames[0].image
                  updateRequired = true
                  self.currentTime = 0.0
               }
               
               if let image, updateRequired {
                  CATransaction.begin()
                  CATransaction.setDisableActions(true)
                  
                  self.layer.contents = image
                  
                  CATransaction.commit()
               }
            } else if self.currentTime == nil {
               CATransaction.begin()
               CATransaction.setDisableActions(true)
               
               self.layer.contents = nil
               
               CATransaction.commit()
               
               self.currentTime = 0.0
            }
         }
      }
   }
}

struct Settings: View {
   @Binding private var resource: String
   @Binding private var changing: Bool
   @Binding private var temperature: Double
   @Binding private var accent: UIColor
   @Binding private var scale: Double
   @Binding private var mute: Bool
   @Environment(\.displayScale) private var displayScale
   @Environment(\.dismiss) private var dismiss
   @Environment(\.openURL) private var openURL
   @State private var paths = [String]()
   @State private var characters = [(String?, String, Bool, CGImage?, Bool)]()
   @State private var purchased = Set<String>()
   @State private var isRestoring = false
   @State private var color: Color
   private var scaleRange: ClosedRange<Double> {
      0.5...max(Double(self.displayScale), 2.0)
   }
   
   var body: some View {
      NavigationStack {
         List {
            self.makeCharacters()
            self.makeAppearance()
            self.makeIntelligence()
         }
         .frame(
            maxWidth: .infinity,
            maxHeight: .infinity
         )
         .background(.clear)
         .scrollContentBackground(.hidden)
         .listStyle(DefaultListStyle())
         .navigationBarTitleDisplayMode(.inline)
         .toolbarBackground(.hidden, for: .navigationBar)
         .toolbar {
            ToolbarItem(placement: .principal) {
               Text("Settings")
                  .foregroundStyle(.primary)
                  .font(.headline)
                  .fontWeight(.semibold)
                  .lineLimit(1)
                  .textCase(.uppercase)
            }
            ToolbarItem(placement: .cancellationAction) {
               Button(action: {
                  dismiss()
               }) {
                  ZStack {
                     Image(systemName: "xmark")
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .foregroundStyle(.primary)
                        .font(
                           .system(size: 8.0)
                        )
                        .bold()
                  }
               }
               .contentShape(Circle())
            }
            ToolbarItem(placement: .primaryAction) {
               Button(action: {
                  openURL(URL(string: "https://milchchan.com/")!)
               }) {
                  ZStack {
                     Image(systemName: "globe")
                        .frame(
                           alignment: .center
                        )
                        .background(.clear)
                        .foregroundStyle(.primary)
                        .font(
                           .system(size: 8.0)
                        )
                        .bold()
                  }
               }
               .contentShape(Circle())
            }
         }
         .transition(.opacity)
         .task {
            let (purchased, characters) = await self.load()
            
            for productID in purchased {
               self.purchased.insert(productID)
            }
            
            withAnimation {
               for (id, path, name, preview) in characters {
                  self.paths.append(path)
                  self.characters.append((id, name, self.resource == path, preview, false))
               }
            }
         }
      }
   }
   
   init(resource: Binding<String>, changing: Binding<Bool>, temperature: Binding<Double>, accent: Binding<UIColor>, scale: Binding<Double>, mute: Binding<Bool>) {
      self._resource = resource
      self._changing = changing
      self._temperature = temperature
      self._accent = accent
      self._scale = scale
      self._mute = mute
      self._color = State(initialValue: Color(uiColor: accent.wrappedValue))
   }
   
   @ViewBuilder
   private func makeCharacters() -> some View {
      Section(header: Text("Characters")
         .foregroundStyle(.primary)
            .fontWeight(.semibold)
            .lineLimit(1)
            .textCase(.uppercase)) {
               ForEach(Array(self.paths.enumerated()), id: \.element) { (index, item) in
                  Button(action: {
                     if !self.changing {
                        if let id = self.characters[index].0, !self.purchased.contains(id) {
                           withAnimation {
                              self.characters[index].4 = true
                           }
                           
                           Task {
                              if let products = try? await Product.products(for: [id]) {
                                 for product in products {
                                    if let purchaseResult = try? await product.purchase(), case .success(.verified(let transaction)) = purchaseResult {
                                       await transaction.finish()
                                       self.purchased.insert(transaction.productID)
                                    }
                                 }
                              }
                              
                              withAnimation {
                                 self.characters[index].4 = false
                              }
                           }
                        } else if self.resource != self.paths[index] {
                           self.resource = self.paths[index]
                           
                           for i in 0..<self.paths.count {
                              if self.paths[i] == self.resource {
                                 dismiss()
                                 
                                 withAnimation {
                                    self.characters[i].2 = true
                                    self.changing = true
                                 }
                              } else {
                                 withAnimation {
                                    self.characters[i].2 = false
                                 }
                              }
                           }
                        }
                     }
                  }) {
                     HStack(alignment: .center, spacing: 16.0) {
                        if let image = self.characters[index].3 {
                           Image(uiImage: UIImage(cgImage: image))
                              .resizable()
                              .scaledToFill()
                              .frame(
                                 width: 24.0,
                                 height: 24.0,
                                 alignment: .top
                              )
                              .background(.clear)
                              .clipShape(Path { path in
                                 let radius = 12.0
                                 let n = 2.5
                                 let k = 8.0 * (1.0 / pow(2.0, 1.0 / n) - 1.0 / 2.0) / 3.0
                                 
                                 path.move(to: CGPointMake(radius, 0.0))
                                 path.addCurve(to: CGPointMake(radius * 2.0, radius), control1: CGPointMake(radius * 2.0 - radius * (1.0 - k), 0.0), control2: CGPointMake(radius * 2.0, radius * (1.0 - k)))
                                 path.addCurve(to: CGPointMake(radius * 2.0 - radius, radius * 2.0), control1: CGPointMake(radius * 2.0, radius * 2.0 - radius * (1.0 - k)), control2: CGPointMake(radius * 2.0 - radius * (1.0 - k), radius * 2.0))
                                 path.addCurve(to: CGPointMake(0.0, radius * 2.0 - radius), control1: CGPointMake(radius * (1.0 - k), radius * 2.0), control2: CGPointMake(0.0, radius * 2.0 - radius * (1.0 - k)))
                                 path.addCurve(to: CGPointMake(radius, 0.0), control1: CGPointMake(0.0, radius * (1.0 - k)), control2: CGPointMake(radius * (1.0 - k), 0.0))
                                 path.closeSubpath()
                              })
                              .overlay(Path { path in
                                 let radius = 12.0
                                 let n = 2.5
                                 let k = 8.0 * (1.0 / pow(2.0, 1.0 / n) - 1.0 / 2.0) / 3.0
                                 
                                 path.move(to: CGPointMake(radius, 0.0))
                                 path.addCurve(to: CGPointMake(radius * 2.0, radius), control1: CGPointMake(radius * 2.0 - radius * (1.0 - k), 0.0), control2: CGPointMake(radius * 2.0, radius * (1.0 - k)))
                                 path.addCurve(to: CGPointMake(radius * 2.0 - radius, radius * 2.0), control1: CGPointMake(radius * 2.0, radius * 2.0 - radius * (1.0 - k)), control2: CGPointMake(radius * 2.0 - radius * (1.0 - k), radius * 2.0))
                                 path.addCurve(to: CGPointMake(0.0, radius * 2.0 - radius), control1: CGPointMake(radius * (1.0 - k), radius * 2.0), control2: CGPointMake(0.0, radius * 2.0 - radius * (1.0 - k)))
                                 path.addCurve(to: CGPointMake(radius, 0.0), control1: CGPointMake(0.0, radius * (1.0 - k)), control2: CGPointMake(radius * (1.0 - k), 0.0))
                                 path.closeSubpath()
                              }.stroke(Color(UIColor.opaqueSeparator), lineWidth: 1.0))
                        } else {
                           Rectangle()
                              .fill(.clear)
                              .frame(
                                 width: 24.0,
                                 height: 24.0
                              )
                        }
                        
                        Text(self.characters[index].1)
                           .foregroundStyle(.primary)
                           .font(.subheadline)
                           .fontWeight(.semibold)
                           .opacity(self.changing ? 0.5 : 1.0)
                           .transition(.opacity.animation(.linear))
                        Spacer()
                        HStack(alignment: .center, spacing: 0.0) {
                           Image(systemName: "checkmark")
                              .frame(
                                 width: 16.0,
                                 height: 16.0,
                                 alignment: .center
                              )
                              .background(.clear)
                              .foregroundStyle(Color(self.accent))
                              .font(
                                 .system(size: 16.0)
                              )
                              .bold()
                              .opacity(self.characters[index].2 && !self.changing ? 1.0 : 0.0)
                              .transition(.opacity.animation(.linear))
                           ProgressView()
                              .progressViewStyle(.circular)
                              .frame(
                                 width: self.characters[index].2 && self.changing || self.characters[index].4 ? nil : 0.0
                              )
                              .padding(EdgeInsets(
                                 top: 0.0,
                                 leading: 0.0,
                                 bottom: 0.0,
                                 trailing: self.characters[index].4 ? 8.0 : 0.0
                              ))
                              .opacity(self.characters[index].2 && self.changing || self.characters[index].4 ? 1.0 : 0.0)
                              .transition(.opacity.animation(.linear))
                           HStack(alignment: .center, spacing: 8.0) {
                              Image(systemName: "cart")
                                 .background(.clear)
                                 .foregroundStyle(Color(uiColor: self.accent))
                                 .font(
                                    .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                                 )
                              Text("Buy")
                                 .foregroundStyle(Color(uiColor: self.accent))
                                 .font(.subheadline)
                                 .fontWeight(.semibold)
                                 .lineLimit(1)
                           }
                           .frame(
                              width: self.characters[index].0 != nil && !self.purchased.contains(self.characters[index].0!) ? nil : 0.0
                           )
                           .clipped()
                           .opacity(self.characters[index].0 != nil && !self.purchased.contains(self.characters[index].0!) ? 1.0 : 0.0)
                           .transition(.opacity.animation(.linear))
                        }
                     }
                     .frame(
                        maxWidth: .infinity
                     )
                     .contentShape(Rectangle())
                  }
                  .buttonStyle(PlainButtonStyle())
                  .disabled(self.characters[index].4)
                  .listRowBackground(Color(uiColor: .systemBackground))
               }
               Button(action: {
                  withAnimation {
                     self.isRestoring = !self.isRestoring
                  }
                  
                  Task {
                     if (try? await AppStore.sync()) != nil {
                        for await verificationResult in Transaction.currentEntitlements {
                           guard case .verified(let transaction) = verificationResult else {
                              continue
                           }
                           
                           if transaction.productType == .nonConsumable && !self.purchased.contains(transaction.productID) {
                              self.purchased.insert(transaction.productID)
                           }
                        }
                     }
                     
                     withAnimation {
                        self.isRestoring = false
                     }
                  }
               }) {
                  HStack(alignment: .center, spacing: 0.0) {
                     ProgressView()
                        .progressViewStyle(.circular)
                        .frame(
                           width: self.isRestoring ? nil : 0.0
                        )
                        .padding(EdgeInsets(
                           top: 0.0,
                           leading: 0.0,
                           bottom: 0.0,
                           trailing: self.isRestoring ? 8.0 : 0.0
                        ))
                        .opacity(self.isRestoring ? 1.0 : 0.0)
                        .transition(.opacity.animation(.linear))
                     Text("Restore")
                        .foregroundStyle(.primary)
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .opacity(self.isRestoring ? 0.5 : 1.0)
                        .transition(.opacity.animation(.linear))
                  }
                  .frame(
                     maxWidth: .infinity,
                     alignment: .center
                  )
                  .contentShape(Rectangle())
               }
               .buttonStyle(PlainButtonStyle())
               .disabled(self.isRestoring)
               .listRowBackground(Color(uiColor: .systemBackground))
               .transition(.opacity.animation(.linear))
            }
      Section {
         VStack(spacing: 8.0) {
            HStack(alignment: .center, spacing: 16.0) {
               Text("Scale")
                  .foregroundStyle(.primary)
                  .font(.subheadline)
                  .fontWeight(.semibold)
               Spacer()
               Text(String(format: "%.1f", self.scale))
                  .foregroundStyle(Color(uiColor: self.accent))
                  .font(.subheadline)
                  .fontWeight(.semibold)
                  .lineLimit(1)
            }
            HStack(alignment: .center, spacing: 0.0) {
               Slider(value: self.$scale, in: self.scaleRange, step: Double.Stride(0.1)) {
                  EmptyView()
               } minimumValueLabel: {
                  Image(systemName: "minus.magnifyingglass")
                     .background(.clear)
                     .foregroundStyle(.primary)
                     .font(
                        .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                     )
               } maximumValueLabel: {
                  Image(systemName: "plus.magnifyingglass")
                     .background(.clear)
                     .foregroundStyle(.primary)
                     .font(
                        .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                     )
               }
               .tint(Color(self.accent))
            }
         }
         .frame(
            maxWidth: .infinity,
            alignment: .center
         )
         .listRowBackground(Color(uiColor: .systemBackground))
         .contentShape(Rectangle())
      }
      Section {
         Toggle("Sounds", isOn: Binding<Bool>(get: { !self.mute }, set: { self.mute = !$0 }))
            .foregroundStyle(.primary)
            .font(.subheadline)
            .fontWeight(.semibold)
            .tint(Color(self.accent))
            .listRowBackground(Color(uiColor: .systemBackground))
      }
   }
   
   private func makeAppearance() -> some View {
      return Section(header: Text("Appearance")
         .foregroundStyle(.primary)
            .fontWeight(.semibold)
            .lineLimit(1)
            .textCase(.uppercase)) {
               ColorPicker("Accent", selection: self.$color, supportsOpacity: false)
                  .foregroundStyle(.primary)
                  .font(.subheadline)
                  .fontWeight(.semibold)
                  .listRowBackground(Color(uiColor: .systemBackground))
                  .contentShape(Rectangle())
                  .onChange(of: self.color) {
                     Task {
                        await Task.detached {
                           await MainActor.run {
                              withAnimation(.linear(duration: 0.5)) {
                                 self.accent = UIColor(self.color)
                              }
                           }
                        }.value
                     }
                  }
               Button(action: {
                  self.color = Color(uiColor: UIColor(named: "AccentColor")!)
               }) {
                  Text("Reset")
                     .foregroundStyle(.primary)
                     .font(.subheadline)
                     .fontWeight(.semibold)
                     .frame(
                        maxWidth: .infinity,
                        alignment: .center
                     )
                     .contentShape(Rectangle())
               }
               .buttonStyle(PlainButtonStyle())
               .listRowBackground(Color(uiColor: .systemBackground))
            }
   }
   
   private func makeIntelligence() -> some View {
      return Section(header: Text("Intelligence")
         .foregroundStyle(.primary)
            .fontWeight(.semibold)
            .lineLimit(1)
            .textCase(.uppercase), footer: Button(action: {
               openURL(URL(string: "https://milchchan.com/privacy")!)
            }) {
               Text("Privacy")
                  .foregroundStyle(Color(self.accent))
                  .font(.footnote)
                  .fontWeight(.semibold)
                  .lineLimit(1)
                  .textCase(.uppercase)
                  .underline()
                  .frame(
                     maxWidth: .infinity,
                     alignment: .center
                  )
                  .padding(EdgeInsets(
                     top: 32.0,
                     leading: 0.0,
                     bottom: 0.0,
                     trailing: 0.0
                  ))
            }) {
               VStack(spacing: 8.0) {
                  HStack(alignment: .center, spacing: 16.0) {
                     Text("Temperature")
                        .foregroundStyle(.primary)
                        .font(.subheadline)
                        .fontWeight(.semibold)
                     Spacer()
                     Text(String(format: "%.1f", self.temperature))
                        .foregroundStyle(Color(uiColor: self.accent))
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .lineLimit(1)
                  }
                  HStack(alignment: .center, spacing: 0.0) {
                     Slider(value: self.$temperature, in: 0.0...2.0, step: Double.Stride(0.1)) {
                        EmptyView()
                     } minimumValueLabel: {
                        Image(systemName: "thermometer.low")
                           .symbolRenderingMode(.monochrome)
                           .background(.clear)
                           .foregroundStyle(.primary)
                           .font(
                              .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                           )
                     } maximumValueLabel: {
                        Image(systemName: "thermometer.high")
                           .symbolRenderingMode(.monochrome)
                           .background(.clear)
                           .foregroundStyle(.primary)
                           .font(
                              .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                           )
                     }
                     .tint(Color(self.accent))
                  }
               }
               .frame(
                  maxWidth: .infinity,
                  alignment: .center
               )
               .listRowBackground(Color(uiColor: .systemBackground))
               .contentShape(Rectangle())
            }
   }
   
   private func load() async -> ([String], [(String?, String, String, CGImage?)]) {
      return await Task.detached {
         var purchased = [String]()
         var resolvedPaths = [(String?, String, String, CGImage?)]()
         
         for await verificationResult in Transaction.currentEntitlements {
            guard case .verified(let transaction) = verificationResult else {
               continue
            }
            
            if transaction.productType == .nonConsumable {
               purchased.append(transaction.productID)
            }
         }
         
         let scale = await Int(round(self.displayScale))
         let parser = Script.Parser()
         var languages = [String?]()
         
         parser.excludeSequences = true
         
         if let preferredLanguage = Locale.preferredLanguages.first {
             let components = Locale.Language.Components(identifier: preferredLanguage)
             
             if let languageCode = components.languageCode {
                 if let script = components.script {
                     languages.append("\(languageCode.identifier)-\(script.identifier)")
                 }
                 
                 languages.append(languageCode.identifier)
             }
         }
         
         languages.append(nil)
         
         if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
            let documentsUrl = containerUrl.appending(path: "Documents", directoryHint: .isDirectory)
            let documentsPath = documentsUrl.path(percentEncoded: false)
            var urlQueue: [(URL, String)] = [(documentsUrl, "Documents")]
            var directories = [String]()
            
            if !FileManager.default.fileExists(atPath: documentsPath) {
               try? FileManager.default.createDirectory(atPath: documentsPath, withIntermediateDirectories: false)
            }
            
            repeat {
               let (baseUrl, basePath) = urlQueue.removeFirst()
               
               if let urls = try? FileManager.default.contentsOfDirectory(at: baseUrl, includingPropertiesForKeys: [.isDirectoryKey, .nameKey], options: .skipsHiddenFiles) {
                  for url in urls {
                     if let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .nameKey]), values.isDirectory ?? false, let name = values.name {
                        let directory = "\(basePath)/\(name)"
                        
                        directories.append(directory)
                        urlQueue.append((baseUrl.appending(path: name, directoryHint: .isDirectory), directory))
                     }
                  }
               }
            } while (!urlQueue.isEmpty)
            
            for directory in directories {
               if let urls = try? FileManager.default.contentsOfDirectory(at: containerUrl.appending(path: directory, directoryHint: .isDirectory), includingPropertiesForKeys: [.nameKey], options: .skipsHiddenFiles) {
                  var paths = [String: [(URL, String, String?, String?, String, String?)]]()
                  
                  for url in urls {
                     if let values = try? url.resourceValues(forKeys: [.nameKey]), let name = values.name, let match = name.wholeMatch(of: /^(.+?)(?:\.([a-z]{2,3}(?:-[A-Z][a-z]{3})?))?\.(?:json|xml)$/) {
                        let key = String(match.output.1)
                        let path = url.path(percentEncoded: false)
                        var characterId: String? = nil
                        var characterName: String? = nil
                        var characterPreview: String? = nil
                        
                        if var tuple = paths[key] {
                           if let output = match.output.2 {
                              var languageCode = String(output)
                              
                              for character in parser.parse(path: path).0 {
                                 if let id = character.id {
                                    characterId = id
                                 }
                                 
                                 if let language = character.language {
                                    languageCode = language
                                 }
                                 
                                 if let preview = character.preview {
                                    characterPreview = preview
                                 }
                                 
                                 characterName = character.name
                              }
                              
                              if let characterName {
                                 tuple.append((url, directory, String(languageCode), characterId, characterName, characterPreview))
                              }
                           } else {
                              for character in parser.parse(path: path).0 {
                                 if let id = character.id {
                                    characterId = id
                                 }
                                 
                                 if let preview = character.preview {
                                    characterPreview = preview
                                 }
                                 
                                 characterName = character.name
                              }
                              
                              if let characterName {
                                 tuple.append((url, directory, nil, characterId, characterName, characterPreview))
                              }
                           }
                           
                           paths[key] = tuple
                        } else if let output = match.output.2 {
                           var languageCode = String(output)
                           
                           for character in parser.parse(path: path).0 {
                              if let id = character.id {
                                 characterId = id
                              }
                              
                              if let language = character.language {
                                 languageCode = language
                              }
                              
                              if let preview = character.preview {
                                 characterPreview = preview
                              }
                              
                              characterName = character.name
                           }
                           
                           if let characterName {
                              paths[key] = [(url, directory, String(languageCode), characterId, characterName, characterPreview)]
                           }
                        } else {
                           for character in parser.parse(path: path).0 {
                              if let id = character.id {
                                 characterId = id
                              }
                              
                              if let preview = character.preview {
                                 characterPreview = preview
                              }
                              
                              characterName = character.name
                           }
                           
                           if let characterName {
                              paths[key] = [(url, directory, nil, characterId, characterName, characterPreview)]
                           }
                        }
                     }
                  }
                  
                  for value in paths.values {
                     var isResolved = false
                     
                     for language in languages {
                        for tuple in value {
                           if tuple.2 == language {
                              var image: CGImage? = nil
                              
                              if let previewPath = tuple.5 {
                                 let imageUrl = tuple.0.deletingLastPathComponent().appending(path: previewPath, directoryHint: .inferFromPath)
                                 
                                 if scale > 1 {
                                    let name = imageUrl.lastPathComponent[imageUrl.lastPathComponent.startIndex..<imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.endIndex, offsetBy: -imageUrl.pathExtension.count - 1)]
                                    let filename = "\(name)@\(scale)\(imageUrl.lastPathComponent[imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.startIndex, offsetBy: name.count)..<imageUrl.lastPathComponent.endIndex])"
                                    let path = imageUrl.deletingLastPathComponent().appending(path: filename, directoryHint: .inferFromPath).path(percentEncoded: false)
                                    
                                    if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                       defer {
                                          try? file.close()
                                       }
                                       
                                       if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                          for i in 0..<CGImageSourceGetCount(imageSource) {
                                             image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                             
                                             break
                                          }
                                       }
                                    }
                                 }
                                 
                                 if image == nil {
                                    let path = imageUrl.path(percentEncoded: false)
                                    
                                    if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                       defer {
                                          try? file.close()
                                       }
                                       
                                       if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                          for i in 0..<CGImageSourceGetCount(imageSource) {
                                             image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                             
                                             break
                                          }
                                       }
                                    }
                                 }
                              }
                              
                              resolvedPaths.append((tuple.3, tuple.1, tuple.4, image))
                              isResolved = true
                           }
                        }
                        
                        if isResolved {
                           break
                        }
                     }
                  }
               }
            }
         }
         
         for resouce in ["Merku", "Milch"] {
            var paths = [String: [(String, String, String?, String?, String, String?)]]()
            
            for path in Bundle.main.paths(forResourcesOfType: "xml", inDirectory: resouce) {
               let input = URL(filePath: path).deletingPathExtension().lastPathComponent
               var characterId: String? = nil
               var characterName: String? = nil
               var characterPreview: String? = nil
               
               if let match = input.wholeMatch(of: /^(.+?)\.([a-z]{2,3}(?:-[A-Z][a-z]{3})?)$/) {
                  let key = String(match.output.1)
                  var languageCode = String(match.output.2)
                  
                  for character in parser.parse(path: path).0 {
                     if let id = character.id {
                        characterId = id
                     }
                     
                     if let language = character.language {
                        languageCode = language
                     }
                     
                     if let preview = character.preview {
                        characterPreview = preview
                     }
                     
                     characterName = character.name
                  }
                  
                  if let characterName {
                     if var tuple = paths[key] {
                        tuple.append((path, resouce, languageCode, characterId, characterName, characterPreview))
                        paths[key] = tuple
                     } else {
                        paths[key] = [(path, resouce, languageCode, characterId, characterName, characterPreview)]
                     }
                  }
               } else {
                  for character in parser.parse(path: path).0 {
                     if let id = character.id {
                        characterId = id
                     }
                     
                     if let preview = character.preview {
                        characterPreview = preview
                     }
                     
                     characterName = character.name
                  }
                  
                  if let characterName {
                     if var tuple = paths[input] {
                        tuple.append((path, resouce, nil, characterId, characterName, characterPreview))
                        paths[input] = tuple
                     } else {
                        paths[input] = [(path, resouce, nil, characterId, characterName, characterPreview)]
                     }
                  }
               }
            }
            
            for value in paths.values {
               var isResolved = false
               
               for language in languages {
                  for tuple in value {
                     if tuple.2 == language {
                        if !resolvedPaths.contains(where: { $0.2 == tuple.4 }) {
                           var image: CGImage? = nil
                           
                           if let previewPath = tuple.5 {
                              let imageUrl = URL(filePath: tuple.0).deletingLastPathComponent().appending(path: previewPath, directoryHint: .inferFromPath)
                              
                              if scale > 1 {
                                 let name = imageUrl.lastPathComponent[imageUrl.lastPathComponent.startIndex..<imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.endIndex, offsetBy: -imageUrl.pathExtension.count - 1)]
                                 let filename = "\(name)@\(scale)\(imageUrl.lastPathComponent[imageUrl.lastPathComponent.index(imageUrl.lastPathComponent.startIndex, offsetBy: name.count)..<imageUrl.lastPathComponent.endIndex])"
                                 let path = imageUrl.deletingLastPathComponent().appending(path: filename, directoryHint: .inferFromPath).path(percentEncoded: false)
                                 
                                 if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                    defer {
                                       try? file.close()
                                    }
                                    
                                    if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                       for i in 0..<CGImageSourceGetCount(imageSource) {
                                          image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                          
                                          break
                                       }
                                    }
                                 }
                              }
                              
                              if image == nil {
                                 let path = imageUrl.path(percentEncoded: false)
                                 
                                 if FileManager.default.fileExists(atPath: path), let file = FileHandle(forReadingAtPath: path) {
                                    defer {
                                       try? file.close()
                                    }
                                    
                                    if let data = try? file.readToEnd(), let imageSource = CGImageSourceCreateWithData(data as CFData, nil) {
                                       for i in 0..<CGImageSourceGetCount(imageSource) {
                                          image = CGImageSourceCreateImageAtIndex(imageSource, i, nil)
                                          
                                          break
                                       }
                                    }
                                 }
                              }
                           }
                           
                           resolvedPaths.append((tuple.3, tuple.1, tuple.4, image))
                        }
                        
                        isResolved = true
                     }
                  }
                  
                  if isResolved {
                     break
                  }
               }
            }
         }
         
         resolvedPaths.sort { $0.2 < $1.2 }
         
         return (purchased, resolvedPaths)
      }.value
   }
}

struct AskIntent: AppIntent {
   static let title: LocalizedStringResource = "Ask"
   static let supportedModes: IntentModes = .background
   
   @Parameter(title: "Prompt")
   var prompt: String
   
   @Parameter(title: "Image", supportedContentTypes: [.image])
   var image: IntentFile?
   
   @MainActor
   func perform() async throws -> some IntentResult & ReturnsValue<String?> {
      let path = AppStorage(wrappedValue: String(), "path").wrappedValue
      var content: String? = nil
      
      for filename in Script.resolve(directory: path.isEmpty ? Double.random(in: 0..<1) < 0.5 ? "Milch" : "Merku" : path) {
         let parser = Script.Parser()
         
         parser.excludeSequences = true
         
         for character in parser.parse(path: filename).0 {
            if let prompt = character.prompt {
               var parts: [[String: Any]] = []
               
               if let image = self.image {
                  if let dataURL = (await Task.detached {
                     var dataURL: String? = nil
                     
                     if let imageSource = CGImageSourceCreateWithData(image.data as CFData, nil), let image = CGImageSourceCreateImageAtIndex(imageSource, 0, nil), let resizedImage = self.resize(image: image) {
                        dataURL = self.convert(image: resizedImage)
                     }
                     
                     return dataURL
                  }.value) {
                     if !self.prompt.isEmpty {
                        parts.append(["type": "text", "text": self.prompt])
                     }
                     
                     parts.append(["type": "image", "image": dataURL])
                  } else {
                     return .result(value: nil)
                  }
               } else {
                  parts.append(["type": "text", "text": self.prompt])
               }
               
               content = await self.generate(messages: [["role": "system", "content": prompt], ["role": "user", "content": parts]], temperature: AppStorage(wrappedValue: 1.0, "temperature").wrappedValue)
            }
            
            break
         }
         
         break
      }
      
      return .result(value: content)
   }
   
   private func generate(messages: [[String: Any]], temperature: Double) async -> String? {
      if let data = try? JSONSerialization.data(withJSONObject: ["messages": messages, "temperature": round(temperature * 10.0) / 10.0]) {
         var request = URLRequest(url: URL(string: "https://milchchan.com/api/generate")!)
         
         request.httpMethod = "POST"
         request.setValue("application/json", forHTTPHeaderField: "Content-Type")
         request.httpBody = data
         request.timeoutInterval = 60.0
         
         if let (data, response) = try? await URLSession.shared.data(for: request), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "application/json", let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [String: Any], let content = jsonRoot["content"] as? String {
            return content
         }
      }
      
      return nil
   }
   
   private nonisolated func resize(image: CGImage, maximum: Double = 768) -> CGImage? {
      let imageWidth = Double(image.width)
      let imageHeight = Double(image.height)
      let width: Double
      let height: Double
      
      if imageWidth < imageHeight {
         if imageHeight > maximum {
            width = floor(maximum / imageHeight * imageWidth)
            height = maximum
         } else {
            width = imageWidth
            height = imageHeight
         }
      } else if imageWidth > maximum {
         width = maximum
         height = floor(maximum / imageWidth * imageHeight)
      } else {
         width = imageWidth
         height = imageHeight
      }
      
      let size = CGSize(width: width, height: height)
      let rendererFormat = UIGraphicsImageRendererFormat()
      
      rendererFormat.opaque = false
      rendererFormat.scale = 1.0
      rendererFormat.preferredRange = .standard
      
      let renderer = UIGraphicsImageRenderer(size: size, format: rendererFormat)
      let resizedImage = renderer.image { rendererContext in
         let context = rendererContext.cgContext
         
         context.interpolationQuality = .high
         context.setAllowsAntialiasing(true)
         context.clear(CGRect(x: 0.0, y: 0.0, width: width, height: height))
         context.translateBy(x: 0.0, y: height)
         context.scaleBy(x: 1.0, y: -1.0)
         context.draw(image, in: CGRect(x: 0.0, y: 0.0, width: width, height: height))
      }.cgImage
      
      return resizedImage
   }
   
   private nonisolated func convert(image: CGImage) -> String? {
      let mutableData = NSMutableData()
      
      guard let destination = CGImageDestinationCreateWithData(mutableData, UTType.jpeg.identifier as CFString, 1, nil) else {
         return nil
      }
      
      CGImageDestinationAddImage(destination, image, [kCGImageDestinationLossyCompressionQuality: 0.75] as CFDictionary)
      
      guard CGImageDestinationFinalize(destination) else {
         return nil
      }
      
      return "data:image/jpeg;base64,\(mutableData.base64EncodedString(options: []))"
   }
}

struct LearnIntent: AppIntent {
   static let title: LocalizedStringResource = "Learn"
   static let supportedModes: IntentModes = .foreground(.dynamic)
   
   @Parameter(title: "Word")
   var word: String?
   
   @MainActor
   func perform() async throws -> some IntentResult {
      if let word = self.word {
         Shortcut.shared.type = ["Dictionary", word]
      } else {
         Shortcut.shared.type = ["Dictionary"]
      }
      
      return .result()
   }
}

struct TalkIntent: AppIntent {
   static let title: LocalizedStringResource = "Talk"
   static let supportedModes: IntentModes = .foreground(.dynamic)
   
   @Parameter(title: "Prompt")
   var prompt: String
   
   @MainActor
   func perform() async throws -> some IntentResult {
      Shortcut.shared.type = [String(), self.prompt]
      
      return .result()
   }
}

#Preview {
   Chat()
}

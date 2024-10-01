//
//  Chat.swift
//  Apricot
//
//  Created by Milch on 2023/01/01.
//

import SwiftUI
import Charts
import AppIntents
import AVFoundation
import Speech
import Vision
import StoreKit
import UIKit

struct Chat: View {
    @StateObject private var shortcut = Shortcut.shared
    @StateObject private var script: Script
    @State private var prompt: (previous: Word?, next: (String?, Word?, Bool, Set<Character>?, Double)?) = (previous: nil, next: nil)
    @State private var logs = [(id: UUID?, from: String?, to: String?, group: Double, content: String)]()
    @State private var labels = [String]()
    @State private var likes = (old: 0, new: [String: [Date]]())
    @State private var revealMenu = false
    @State private var showActivity = false
    @State private var showDictionary = false
    @State private var showSettings = false
    @State private var selection: String
    @State private var isLongPressed = false
    @State private var isRecording = false
    @State private var isChanging = false
    @State private var isLoading = false
    @State private var loadingAmount = 0.0
    @State private var intensity = 0.5
    @State private var volumeLevel = 0.0
    @State private var audioEngine: AVAudioEngine? = nil
    @State private var speechRecognizer: SFSpeechRecognizer? = nil
    @State private var speechAudioBufferRecognitionRequest: SFSpeechAudioBufferRecognitionRequest? = nil
    @State private var speechRecognitionTask: SFSpeechRecognitionTask? = nil
    private var path: AppStorage<String>
    private var accent: AppStorage<String>
    @AppStorage("types") private var types = 0
    @AppStorage("scale") private var scale = 1.0
    @AppStorage("mute") private var mute = false
    @AppStorage("temperature") private var temperature = 1.0
    private let starImage: UIImage
    
    var body: some View {
        ZStack {
            VStack {
                Stage(prompt: self.$prompt, logs: self.$logs, resource: Binding<(old: String, new: String)>(get: { (old: self.selection, new: self.path.wrappedValue) }, set: { newValue in
                    self.selection = newValue.old
                    self.path.wrappedValue = newValue.new
                }), attributes: self.$script.attributes, types: self.$types, labels: self.$labels, likes: self.$likes, changing: self.$isChanging, loading: self.$isLoading, intensity: self.$intensity, temperature: self.temperature, accent: self.convert(from: self.accent.wrappedValue), scale: self.scale, pause: self.revealMenu || self.showActivity || self.showDictionary || self.showSettings, mute: self.mute)
                    .frame(
                        minWidth: 0.0,
                        maxWidth: .infinity,
                        minHeight: 0.0,
                        maxHeight: .infinity
                    )
                    .background(.clear)
                    .ignoresSafeArea(.all)
            }
            VStack(spacing: 0.0) {
                VStack(spacing: 16.0) {
                    Image(uiImage: self.starImage.withRenderingMode(.alwaysTemplate))
                        .padding(EdgeInsets(
                            top: 8.0,
                            leading: 0.0,
                            bottom: 0.0,
                            trailing: 0.0
                        ))
                        .background(.clear)
                        .foregroundColor(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                        }))
                    Text(String(format: "%ld", self.likes.old))
                        .frame(
                            alignment: .top
                        )
                        .offset(y: -floor(UIFont(name: "DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0))!.ascender - UIFont(name: "DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0))!.capHeight))
                        .background(.clear)
                        .foregroundColor(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                        }))
                        .font(.custom("DIN2014-Demi", size: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .headline).pointSize * 5.0)))
                        .lineLimit(1)
                        .truncationMode(.tail)
                        .contentTransition(.numericText(value: Double(self.likes.old)))
                    
                    if self.isLoading {
                        HStack(spacing: 8.0) {
                            ForEach(0..<3) { index in
                                Circle()
                                    .frame(width: 8, height: 8)
                                    .foregroundColor(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
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
            GeometryReader { geometry in
                VStack(spacing: 0.0) {
                    Spacer()
                        .frame(
                            minHeight: 0.0
                        )
                    ZStack(alignment: .bottom) {
                        if self.revealMenu {
                            VStack(spacing: 0.0) {
                                VStack(spacing: 0.0) {
                                    ScrollView([.vertical]) {
                                        ZStack {
                                            Spacer()
                                                .containerRelativeFrame([.vertical])
                                            LazyVStack(spacing: 0.0) {
                                                VStack(spacing: 0.0) {
                                                    Button(action: {
                                                        if let prompt = self.prompt.next, let word = prompt.1 {
                                                            if prompt.2 {
                                                                self.prompt = (previous: self.prompt.previous, next: (prompt.0, prompt.1, word.attributes.isEmpty, prompt.3, CACurrentMediaTime()))
                                                            } else {
                                                                Task {
                                                                    await self.talk(word: word, intensity: self.intensity, temperature: self.temperature, multiple: UIDevice.current.orientation.isLandscape)
                                                                }
                                                                
                                                                self.prompt = (previous: word, next: nil)
                                                                
                                                                withAnimation(.easeInOut(duration: 0.5)) {
                                                                    self.revealMenu = false
                                                                }
                                                            }
                                                        }
                                                    }) {
                                                        ZStack(alignment: .center) {
                                                            Prompt(input: self.prompt.next, accent: self.convert(from: self.accent.wrappedValue), font: UIFont.systemFont(ofSize: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0), weight: .semibold))
                                                                .frame(
                                                                    height: ceil(UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).lineHeight),
                                                                    alignment: .center
                                                                )
                                                                .offset(y: ceil(UIFont.systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0, weight: .semibold).leading / 2.0))
                                                                .padding(0.0)
                                                                .background(.clear)
                                                            
                                                            if self.script.words.isEmpty {
                                                                Image(systemName: "exclamationmark.triangle")
                                                                    .frame(
                                                                        width: 16.0,
                                                                        height: 16.0,
                                                                        alignment: .center
                                                                    )
                                                                    .background(.clear)
                                                                    .foregroundColor(Color(UIColor {
                                                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                    }))
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
                                                    Text(String(format: "%ld", self.script.words.count))
                                                        .foregroundColor(Color(UIColor {
                                                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                        }))
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
                                                        .animation(.linear(duration: 0.5), value: "\(self.script.words.count)")
                                                    Button(action: {
                                                        if let prompt = self.prompt.next {
                                                            if let word = prompt.1, !word.attributes.isEmpty && prompt.2 {
                                                                self.prompt = (previous: nil, next: (prompt.0, prompt.1, false, prompt.3, CACurrentMediaTime()))
                                                            } else if !self.script.words.isEmpty {
                                                                let samples = 10
                                                                var letterSet: Set<Character> = []
                                                                var modifiers: [String] = []
                                                                var words: [Word] = []
                                                                
                                                                for _ in 0..<samples {
                                                                    let word = self.script.words[Int.random(in: 0..<self.script.words.count)]
                                                                    
                                                                    for i in 0..<word.name.count {
                                                                        let character = word.name[word.name.index(word.name.startIndex, offsetBy: i)]
                                                                        
                                                                        if !letterSet.contains(character) && !character.isNewline && !character.isWhitespace {
                                                                            letterSet.insert(character)
                                                                        }
                                                                    }
                                                                    
                                                                    if word.attributes.isEmpty {
                                                                        modifiers.append(word.name)
                                                                    } else {
                                                                        words.append(word)
                                                                    }
                                                                }
                                                                
                                                                if words.isEmpty {
                                                                    if modifiers.isEmpty {
                                                                        self.prompt = (previous: nil, next: (nil, nil, false, nil, CACurrentMediaTime()))
                                                                    } else {
                                                                        let modifier = modifiers[Int.random(in: 0..<modifiers.count)]
                                                                        
                                                                        self.prompt = (previous: nil, next: (modifier, Word(name: modifier), true, letterSet, CACurrentMediaTime()))
                                                                    }
                                                                } else {
                                                                    let epsilon = powl(10, -6)
                                                                    var probabilities = self.softmax(x: words.reduce(into: [], { x, y in
                                                                        if let (score, _, _, _) = Script.shared.scores[y.name] {
                                                                            x.append(score)
                                                                        } else {
                                                                            x.append(epsilon)
                                                                        }
                                                                    }), temperature: self.temperature)
                                                                    var word = words[min(self.choice(probabilities: probabilities), probabilities.count - 1)]
                                                                    
                                                                    if Double.random(in: 0.0..<1.0) < Double(modifiers.count) / Double(samples) {
                                                                        probabilities = self.softmax(x: modifiers.reduce(into: [], { x, y in
                                                                            if let (score, _, _, _) = Script.shared.scores[y] {
                                                                                x.append(score)
                                                                            } else {
                                                                                x.append(epsilon)
                                                                            }
                                                                        }), temperature: self.temperature)
                                                                        word.name = modifiers[min(self.choice(probabilities: probabilities), probabilities.count - 1)] + word.name
                                                                    }
                                                                    
                                                                    self.prompt = (previous: nil, next: (word.name, word, false, letterSet, CACurrentMediaTime()))
                                                                }
                                                            }
                                                        }
                                                    }) {
                                                        VStack(alignment: .center, spacing: 8.0) {
                                                            Image(systemName: "dice")
                                                                .frame(
                                                                    width: 16.0,
                                                                    height: 16.0,
                                                                    alignment: .center
                                                                )
                                                                .background(.clear)
                                                                .foregroundColor(Color(UIColor {
                                                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                }))
                                                                .font(
                                                                    .system(size: 16.0)
                                                                )
                                                                .bold()
                                                            Text("Randomize")
                                                                .foregroundColor(Color(UIColor {
                                                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                }))
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
                                                .padding(0.0)
                                                .background(.clear)
                                                .foregroundColor(.black)
                                                Rectangle()
                                                    .frame(
                                                        height: 1.0
                                                    )
                                                    .foregroundColor(Color(UIColor {
                                                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 0.25) : UIColor(white: 1.0, alpha: 0.25)
                                                    }))
                                                HStack(alignment: .center, spacing: 0.0) {
                                                    VStack(alignment: .center, spacing: 0.0) {
                                                        Button(action: {
                                                            self.showActivity = true
                                                            self.prompt = (previous: self.prompt.previous, next: nil)
                                                            
                                                            withAnimation(.easeInOut(duration: 0.5)) {
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
                                                                    .foregroundColor(Color(UIColor {
                                                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                    }))
                                                                    .font(
                                                                        .system(size: 16.0)
                                                                    )
                                                                    .bold()
                                                                Text("Activity")
                                                                    .foregroundColor(Color(UIColor {
                                                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                    }))
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
                                                        .frame(
                                                            width: 1.0
                                                        )
                                                        .foregroundColor(Color(UIColor {
                                                            $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 0.25) : UIColor(white: 1.0, alpha: 0.25)
                                                        }))
                                                    VStack(alignment: .center, spacing: 0.0) {
                                                        Button(action: {
                                                            self.showDictionary = true
                                                            self.prompt = (previous: self.prompt.previous, next: nil)
                                                            
                                                            withAnimation(.easeInOut(duration: 0.5)) {
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
                                                                    .foregroundColor(Color(UIColor {
                                                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                    }))
                                                                    .font(
                                                                        .system(size: 16.0)
                                                                    )
                                                                    .bold()
                                                                Text("Dictionary")
                                                                    .foregroundColor(Color(UIColor {
                                                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                    }))
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
                                                        .frame(
                                                            width: 1.0
                                                        )
                                                        .foregroundColor(Color(UIColor {
                                                            $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 0.25) : UIColor(white: 1.0, alpha: 0.25)
                                                        }))
                                                    VStack(alignment: .center, spacing: 0.0) {
                                                        Button(action: {
                                                            self.showSettings = true
                                                            self.prompt = (previous: self.prompt.previous, next: nil)
                                                            
                                                            withAnimation(.easeInOut(duration: 0.5)) {
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
                                                                    .foregroundColor(Color(UIColor {
                                                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                    }))
                                                                    .font(
                                                                        .system(size: 16.0)
                                                                    )
                                                                    .bold()
                                                                Text("Settings")
                                                                    .foregroundColor(Color(UIColor {
                                                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                    }))
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
                                                ForEach(self.labels.indices, id: \.self) { index in
                                                    let type = self.labels[index]
                                                    let checked = self.types & Int(pow(2.0, Double(index)))
                                                    
                                                    VStack(spacing: 0.0) {
                                                        Rectangle()
                                                            .frame(
                                                                height: 1.0
                                                            )
                                                            .foregroundColor(Color(UIColor {
                                                                $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 0.25) : UIColor(white: 1.0, alpha: 0.25)
                                                            }))
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
                                                            HStack(alignment: .center, spacing: 16.0) {
                                                                if checked > 0 {
                                                                    Image(systemName: "checkmark")
                                                                        .frame(
                                                                            width: 16.0,
                                                                            height: 16.0,
                                                                            alignment: .center
                                                                        )
                                                                        .background(.clear)
                                                                        .foregroundColor(Color(self.convert(from: self.accent.wrappedValue)))
                                                                        .font(
                                                                            .system(size: 16.0)
                                                                        )
                                                                        .bold()
                                                                        .transition(AnyTransition.opacity.combined(with: .scale))
                                                                }
                                                                
                                                                Text(type)
                                                                    .foregroundColor(Color(UIColor {
                                                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                                                    }))
                                                                    .font(.subheadline)
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
                                                    .transition(.opacity)
                                                }
                                            }
                                            .padding(0.0)
                                            .background(.clear)
                                        }
                                    }
                                    .frame(
                                        height: UIScreen.main.bounds.height / 2.0 - 64.0 - geometry.safeAreaInsets.bottom
                                    )
                                    .padding(EdgeInsets(
                                        top: 0.0,
                                        leading: 0.0,
                                        bottom: 0.0,
                                        trailing: 0.0
                                    ))
                                    .background(
                                        RoundedRectangle(cornerRadius: 16.0)
                                            .foregroundStyle(.ultraThinMaterial)
                                            .shadow(color: Color(UIColor(white: 0.0, alpha: 0.25)), radius: 8.0, x: 0.0, y: 0.0)
                                    )
                                }
                                .padding(EdgeInsets(
                                    top: 0.0,
                                    leading: 16.0,
                                    bottom: 64.0,
                                    trailing: 16.0
                                ))
                                .background(.clear)
                                Spacer()
                                    .frame(
                                        maxWidth: .infinity
                                    )
                                    .frame(
                                        height: geometry.safeAreaInsets.bottom
                                    )
                                    .background(.clear)
                            }
                            .background(.clear)
                            .transition(AnyTransition.opacity.combined(with: .move(edge: .bottom)))
                        }
                        VStack(spacing: 0.0) {
                            Button(action: {
                                if self.isLongPressed {
                                    self.isLongPressed = false
                                } else if self.isRecording {
                                    self.stopRecognize()
                                } else {
                                    if self.revealMenu {
                                        self.prompt = (previous: self.prompt.previous, next: nil)
                                    } else if let previous = self.prompt.previous {
                                        var letterSet: Set<Character> = []
                                        
                                        for i in 0..<previous.name.count {
                                            let character = previous.name[previous.name.index(previous.name.startIndex, offsetBy: i)]
                                            
                                            if !letterSet.contains(character) && !character.isNewline && !character.isWhitespace {
                                                letterSet.insert(character)
                                            }
                                        }
                                        
                                        self.prompt = (previous: previous, next: (previous.name, previous, false, letterSet, CACurrentMediaTime()))
                                    } else if self.script.words.isEmpty {
                                        self.prompt = (previous: nil, next: (nil, nil, false, nil, CACurrentMediaTime()))
                                    } else {
                                        let samples = 10
                                        var letterSet: Set<Character> = []
                                        var modifiers: [String] = []
                                        var words: [Word] = []
                                        
                                        for _ in 0..<samples {
                                            let word = self.script.words[Int.random(in: 0..<self.script.words.count)]
                                            
                                            for i in 0..<word.name.count {
                                                let character = word.name[word.name.index(word.name.startIndex, offsetBy: i)]
                                                
                                                if !letterSet.contains(character) && !character.isNewline && !character.isWhitespace {
                                                    letterSet.insert(character)
                                                }
                                            }
                                            
                                            if word.attributes.isEmpty {
                                                modifiers.append(word.name)
                                            } else {
                                                words.append(word)
                                            }
                                        }
                                        
                                        if words.isEmpty {
                                            if modifiers.isEmpty {
                                                self.prompt = (previous: nil, next: (nil, nil, false, nil, CACurrentMediaTime()))
                                            } else {
                                                let modifier = modifiers[Int.random(in: 0..<modifiers.count)]
                                                
                                                self.prompt = (previous: nil, next: (modifier, Word(name: modifier), true, letterSet, CACurrentMediaTime()))
                                            }
                                        } else {
                                            let epsilon = powl(10, -6)
                                            var probabilities = self.softmax(x: words.reduce(into: [], { x, y in
                                                if let (score, _, _, _) = Script.shared.scores[y.name] {
                                                    x.append(score)
                                                } else {
                                                    x.append(epsilon)
                                                }
                                            }), temperature: self.temperature)
                                            var word = words[min(self.choice(probabilities: probabilities), probabilities.count - 1)]
                                            
                                            if Double.random(in: 0.0..<1.0) < Double(modifiers.count) / Double(samples) {
                                                probabilities = self.softmax(x: modifiers.reduce(into: [], { x, y in
                                                    if let (score, _, _, _) = Script.shared.scores[y] {
                                                        x.append(score)
                                                    } else {
                                                        x.append(epsilon)
                                                    }
                                                }), temperature: self.temperature)
                                                word.name = modifiers[min(self.choice(probabilities: probabilities), probabilities.count - 1)] + word.name
                                            }
                                            
                                            self.prompt = (previous: nil, next: (word.name, word, true, letterSet, CACurrentMediaTime()))
                                        }
                                    }
                                    
                                    withAnimation(.easeInOut(duration: 0.5)) {
                                        self.revealMenu.toggle()
                                    }
                                }
                            }) {
                                if self.isRecording {
                                    Image(systemName: "mic")
                                        .frame(
                                            width: 16.0,
                                            height: 16.0,
                                            alignment: .center
                                        )
                                        .background(.clear)
                                        .foregroundColor(Color(self.convert(from: self.accent.wrappedValue)))
                                        .font(
                                            .system(size: 16.0)
                                        )
                                        .bold()
                                        .padding(16.0)
                                        .opacity(0.5 + 0.5 * (1.0 - self.volumeLevel))
                                        .transition(.opacity)
                                } else if self.revealMenu {
                                    Image(systemName: "chevron.down")
                                        .frame(
                                            width: 16.0,
                                            height: 16.0,
                                            alignment: .center
                                        )
                                        .background(.clear)
                                        .foregroundColor(Color(UIColor {
                                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                        }))
                                        .font(
                                            .system(size: 16.0)
                                        )
                                        .bold()
                                        .padding(16.0)
                                        .transition(.opacity)
                                } else {
                                    Image(systemName: "chevron.up")
                                        .frame(
                                            width: 16.0,
                                            height: 16.0,
                                            alignment: .center
                                        )
                                        .background(.clear)
                                        .foregroundColor(Color(UIColor {
                                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                        }))
                                        .font(
                                            .system(size: 16.0)
                                        )
                                        .bold()
                                        .padding(16.0)
                                        .transition(.opacity)
                                }
                            }
                            .frame(
                                alignment: .center
                            )
                            .background(
                                RoundedRectangle(cornerRadius: .infinity)
                                    .foregroundStyle(.ultraThinMaterial)
                                    .shadow(color: Color(UIColor(white: 0.0, alpha: 0.25)), radius: 8.0, x: 0.0, y: 0.0)
                            )
                            .simultaneousGesture(LongPressGesture().onEnded{ _ in
                                if let first = Script.shared.characters.first, first.prompt != nil && !self.revealMenu && !self.isRecording {
                                    self.startRecognize()
                                    self.isLongPressed = true
                                }
                            })
                            Spacer()
                                .frame(
                                    maxWidth: .infinity
                                )
                                .frame(
                                    height: geometry.safeAreaInsets.bottom == 0.0 ? 8.0 : geometry.safeAreaInsets.bottom
                                )
                                .background(.clear)
                        }
                        .background(.clear)
                    }
                    .background(.clear)
                    .sheet(isPresented: self.$showActivity, content: {
                        Activity(accent: self.convert(from: self.accent.wrappedValue), data: Script.shared.characters.reduce(into: [], { x, y in
                            if !y.guest {
                                var sequences: [Sequence] = []
                                
                                for sequence in y.sequences {
                                    if sequence.name == "Like" {
                                        sequences.append(sequence)
                                    }
                                }
                                
                                x.append((name: y.name, sequences: sequences, likes: self.likes.new[y.name]))
                            }
                        }), logs: self.$logs)
                            .presentationBackground(.ultraThinMaterial)
                            .presentationDetents([.large])
                    })
                    .sheet(isPresented: self.$showDictionary, content: {
                        Dictionary(accent: self.convert(from: self.accent.wrappedValue), type: Binding<String?>(get: {
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
        }
        .frame(
            minWidth: 0.0,
            maxWidth: .infinity,
            minHeight: 0.0,
            maxHeight: .infinity,
            alignment: .topLeading
        )
        .background(Color(UIColor {
            $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
        }))
        .onChange(of: self.shortcut.type) {
            if let type = self.shortcut.type, !type.isEmpty {
                if type[0].isEmpty {
                    var word = Word(name: type[1])
                    
                    for attribute in Script.shared.words.reduce(into: Set<String>(), { x, y in
                        if y.name == type[1] {
                            for attribute in y.attributes {
                                if !x.contains(attribute) {
                                    x.insert(attribute)
                                }
                            }
                        }
                    }) {
                        word.attributes.append(attribute)
                    }
                    
                    Task {
                        await self.talk(word: word, intensity: self.intensity, temperature: self.temperature, multiple: UIDevice.current.orientation.isLandscape, fallback: false)
                    }
                    
                    self.showActivity = false
                    self.showSettings = false
                    self.showDictionary = false
                    self.shortcut.type = nil
                } else if type[0] == "Dictionary" {
                    if self.isRecording {
                        self.stopRecognize()
                    }
                    
                    self.showActivity = false
                    self.showSettings = false
                    self.showDictionary = true
                    
                    if self.revealMenu {
                        withAnimation(.easeInOut(duration: 0.5)) {
                            self.revealMenu = false
                        }
                    }
                }
            }
        }
        .onAppear {
            if let type = self.shortcut.type, !type.isEmpty {
                if type[0].isEmpty {
                    var word = Word(name: type[1])
                    
                    for attribute in Script.shared.words.reduce(into: Set<String>(), { x, y in
                        if y.name == type[1] {
                            for attribute in y.attributes {
                                if !x.contains(attribute) {
                                    x.insert(attribute)
                                }
                            }
                        }
                    }) {
                        word.attributes.append(attribute)
                    }
                    
                    Task {
                        await self.talk(word: word, intensity: self.intensity, temperature: self.temperature, multiple: UIDevice.current.orientation.isLandscape, fallback: false)
                    }
                    
                    self.shortcut.type = nil
                } else if type[0] == "Dictionary" {
                    self.showDictionary = true
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
        
        self._script = StateObject(wrappedValue: Script.shared)
        self.path = AppStorage(wrappedValue: String(), "path")
        self._selection = State(initialValue: self.path.wrappedValue)
        self.accent = AppStorage(wrappedValue: String.init(format: "#%02lx%02lx%02lx", lroundf(Float(red * 255)), lroundf(Float(green * 255)), lroundf(Float(blue * 255))), "accent")
        
        UIGraphicsBeginImageContextWithOptions(size, false, 1.0)
        
        if let context = UIGraphicsGetCurrentContext(), let starImage = image.cgImage {
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
        
        UIGraphicsEndImageContext()
        
        self.starImage = image
    }
    
    private func talk(word: Word, intensity: Double, temperature: Double, multiple: Bool, fallback: Bool = true) async {
        var queue = Script.shared.characters
        
        if let first = queue.first {
            var logs = [(id: UUID?, from: String?, to: String?, group: Double, content: String)]()
            var generateRequired: Bool
            var input: String
            var time: Double
            var sequences = [(String, UUID?, Sequence)]()
            
            if multiple {
                queue.removeFirst()
                
                if let last = self.logs.last {
                    var isContinuous = false
                    
                    for log in self.logs {
                        if log.group == last.group {
                            if log.from == nil && log.content == word.name {
                                isContinuous = true
                            }
                            
                            logs.append(log)
                        }
                    }
                    
                    if isContinuous {
                        generateRequired = true
                        input = String()
                        time = last.group
                    } else {
                        logs.removeAll()
                        generateRequired = word.attributes.isEmpty ? true : Double.random(in: 0..<1) < intensity
                        input = word.name
                        time = CACurrentMediaTime()
                    }
                } else {
                    generateRequired = word.attributes.isEmpty ? true : Double.random(in: 0..<1) < intensity
                    input = word.name
                    time = CACurrentMediaTime()
                }
            } else {
                queue.removeAll()
                
                if let last = self.logs.last, self.logs.contains(where: { $0.from == nil && $0.group == last.group && $0.content == word.name }) {
                    generateRequired = true
                    input = String()
                    time = last.group
                } else {
                    generateRequired = word.attributes.isEmpty ? true : Double.random(in: 0..<1) < intensity
                    input = word.name
                    time = CACurrentMediaTime()
                }
            }
            
            if let prompt = first.prompt, generateRequired {
                withAnimation(.easeOut(duration: 0.5)) {
                    self.isLoading = true
                }
                
                var messages: [[String: Any]] = [["role": "system", "content": prompt]]
                var i = logs.count - 1
                
                while i > 0 {
                    if let from = logs[i].from, from != first.name && logs[i - 1].from == first.name {
                        messages.insert(["role": "user", "content": logs[i].content], at: 1)
                        messages.insert(["role": "assistant", "content": logs[i - 1].content], at: 1)
                        
                        i -= 2
                    } else {
                        i -= 1
                    }
                }
                
                if messages.count == 1 {
                    var i = self.logs.count - 1
                    
                    while i > 0 {
                        if self.logs[i].from == first.name && self.logs[i].to == nil && self.logs[i - 1].from == nil && self.logs[i - 1].to == first.name {
                            messages.insert(["role": "assistant", "content": self.logs[i].content], at: 1)
                            messages.insert(["role": "user", "content": self.logs[i - 1].content], at: 1)
                            
                            i -= 2
                        } else {
                            i -= 1
                        }
                    }
                    
                    messages.append(["role": "user", "content": input])
                }
                
                if let (content, state) = await self.generate(messages: messages, temperature: temperature) {
                    var text = String(content)
                    let sequence = Sequence(name: "Activate", state: nil)
                    let id = UUID()
                    var inlines = [(text: String, attributes: [String]?)]()
                    
                    while !text.isEmpty {
                        if !word.attributes.isEmpty, let range = text.range(of: word.name) {
                            if text.startIndex != range.lowerBound {
                                inlines.append((text: String(text[text.startIndex..<range.lowerBound]), attributes: nil))
                            }
                            
                            inlines.append((text: String(text[range.lowerBound..<range.upperBound]), attributes: word.attributes))
                            text = String(text[range.upperBound..<text.endIndex])
                        } else {
                            inlines.append((text: text, attributes: nil))
                            text.removeAll()
                        }
                    }
                    
                    sequence.append(Message(id: id, inlines: inlines))
                    sequence.append(Sequence(name: "Emote", state: state ?? String()))
                    sequences.append((first.name, id, sequence))
                    
                    while !queue.isEmpty {
                        let character = queue.removeFirst()
                        
                        if let prompt = character.prompt {
                            var messages: [[String: Any]] = [["role": "system", "content": prompt], ["role": "user", "content": content]]
                            var i = logs.count - 1
                            
                            while i > 0 {
                                if logs[i].from == character.name {
                                    messages.insert(["role": "assistant", "content": logs[i].content], at: 1)
                                    messages.insert(["role": "user", "content": logs[i - 1].content], at: 1)
                                    
                                    i -= 2
                                } else {
                                    i -= 1
                                }
                            }
                            
                            if let (content, state) = await self.generate(messages: messages, temperature: temperature) {
                                var text = String(content)
                                let sequence = Sequence(name: "Activate", state: nil)
                                let id = UUID()
                                var inlines = [(text: String, attributes: [String]?)]()
                                
                                while !text.isEmpty {
                                    if !word.attributes.isEmpty, let range = text.range(of: word.name) {
                                        if text.startIndex != range.lowerBound {
                                            inlines.append((text: String(text[text.startIndex..<range.lowerBound]), attributes: nil))
                                        }
                                        
                                        inlines.append((text: String(text[range.lowerBound..<range.upperBound]), attributes: word.attributes))
                                        text = String(text[range.upperBound..<text.endIndex])
                                    } else {
                                        inlines.append((text: text, attributes: nil))
                                        text.removeAll()
                                    }
                                }
                                
                                sequence.append(Message(id: id, inlines: inlines))
                                sequence.append(Sequence(name: "Emote", state: state ?? String()))
                                sequences.append((character.name, id, sequence))
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
                    self.isLoading = false
                }
            } else {
                queue.removeAll()
            }
            
            if sequences.isEmpty {
                let sequences = Script.shared.characters.reduce(into: [Sequence](), { x, y in
                    if y.name == first.name {
                        for sequence in y.sequences {
                            if sequence.name == "Activate" {
                                x.append(sequence)
                            }
                        }
                    }
                })
                var isEmpty = true
                
                await Script.shared.run(name: first.name, sequences: sequences, words: [word], temperature: temperature) { x in
                    if !x.isEmpty {
                        var y = x
                        var content = [String]()
                        
                        for sequence in x {
                            for obj in sequence {
                                if let message = obj as? Message {
                                    content.append(message.content)
                                }
                            }
                        }
                        
                        y.append(Sequence(name: String()))
                        
                        self.logs.append((id: nil, from: nil, to: first.name, group: time, content: word.name))
                        self.logs.append((id: nil, from: first.name, to: nil, group: time, content: content.joined(separator: "\n")))
                        
                        isEmpty = false
                        
                        return y
                    }
                    
                    return x
                }
                
                if fallback && isEmpty {
                    await Script.shared.run(name: first.name, sequences: sequences, words: [])
                }
            } else {
                for i in 0..<sequences.count {
                    await Script.shared.run(name: sequences[i].0, sequences: [sequences[i].2], words: []) { x in
                        var y = x
                        var content = [String]()
                        
                        for sequence in x {
                            for obj in sequence {
                                if let message = obj as? Message {
                                    content.append(message.content)
                                }
                            }
                        }
                        
                        y.append(Sequence(name: String()))
                        
                        if i > 0 {
                            self.logs.append((id: sequences[i].1, from: sequences[i].0, to: sequences[0].0, group: time, content: content.joined(separator: "\n")))
                        } else {
                            if !input.isEmpty {
                                self.logs.append((id: nil, from: nil, to: sequences[i].0, group: time, content: word.name))
                            }
                            
                            self.logs.append((id: sequences[i].1, from: sequences[i].0, to: nil, group: time, content: content.joined(separator: "\n")))
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
    
    private func generate(messages: [[String: Any]], temperature: Double) async -> (String, String?)? {
        if let data = try? JSONSerialization.data(withJSONObject: messages) {
            var request = URLRequest(url: URL(string: "https://milchchan.com/api/generate?temperature=\(String(format: "%.1f", temperature))".addingPercentEncoding(withAllowedCharacters: .urlQueryAllowed)!)!)
            
            request.httpMethod = "POST"
            request.setValue("application/json", forHTTPHeaderField: "Content-Type")
            request.httpBody = data
            
            if let (data, response) = try? await URLSession.shared.data(for: request), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "application/json", let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [String: Any], let content = jsonRoot["content"] as? String {
                var state: String? = nil
                
                if let states = jsonRoot["states"] as? [String: Any] {
                    var max = 0.0
                    
                    for (key, object) in states {
                        if let value = object as? Double, value > max {
                            state = key
                            max = value
                        }
                    }
                }
                
                return (content, state)
            }
        }
        
        return nil
    }
    
    private func startRecognize() {
        withAnimation(.easeInOut(duration: 0.5)) {
            self.isRecording = true
        }
        
        self.speechRecognizer = SFSpeechRecognizer()
        
        Task {
            if await withCheckedContinuation({ continuation in
                SFSpeechRecognizer.requestAuthorization { status in
                    continuation.resume(returning: status == .authorized)
                }
            }), await AVAudioApplication.requestRecordPermission(), await (Task.detached {
                let audioSession = AVAudioSession.sharedInstance()
                
                if audioSession.category != .playAndRecord {
                    do {
                        try audioSession.setCategory(.playAndRecord, mode: .measurement, options: .duckOthers)
                        try audioSession.setActive(true, options: .notifyOthersOnDeactivation)
                    } catch {
                        return false
                    }
                }
                
                return true
            }.value), let (audioEngine, inputNode) = await {
                let audioEngine = AVAudioEngine()
                
                return (audioEngine, await Task.detached { audioEngine.inputNode }.value)
            }(), let recognizer = self.speechRecognizer, recognizer.isAvailable {
                let request = SFSpeechAudioBufferRecognitionRequest()
                
                request.shouldReportPartialResults = false
                
                inputNode.installTap(onBus: 0, bufferSize: 1024, format: inputNode.outputFormat(forBus: 0)) { (buffer: AVAudioPCMBuffer, when: AVAudioTime) in
                    request.append(buffer)
                    
                    if let floatChannelData = buffer.floatChannelData {
                        let pointee = floatChannelData.pointee
                        let rms = sqrt(stride(from: 0, to: Int(buffer.frameLength), by: buffer.stride).map { pointee[$0] }.map { $0 * $0 }.reduce(0, +) / Float(buffer.frameLength))
                        let dB = rms == 0.0 ? 0.0 : 20.0 * log10(rms)
                        let minimum: Float = -50.0
                        let maximum: Float = -25.0
                        let level = Double(dB > maximum ? 1.0 : (abs(minimum) - abs(max(dB, minimum))) / (abs(minimum) - abs(maximum)))
                        
                        Task {
                            await MainActor.run {
                                withAnimation(.easeInOut(duration: 0.5)) {
                                    self.volumeLevel = level
                                }
                            }
                        }
                    }
                }
                
                audioEngine.prepare()
                
                do {
                    try audioEngine.start()
                } catch {
                    self.speechRecognizer = nil
                    
                    withAnimation(.easeInOut(duration: 0.5)) {
                        self.isRecording = false
                    }
                    
                    return
                }
                
                self.audioEngine = audioEngine
                self.speechAudioBufferRecognitionRequest = request
                self.speechRecognitionTask = recognizer.recognitionTask(with: request, resultHandler: { result, error in
                    if error == nil {
                        if let result {
                            let text = result.bestTranscription.formattedString
                            
                            if result.isFinal && audioEngine.isRunning {
                                audioEngine.stop()
                                audioEngine.inputNode.removeTap(onBus: 0)
                                
                                withAnimation(.easeInOut(duration: 0.5)) {
                                    self.volumeLevel = 0.0
                                }
                            }
                            
                            if !text.isEmpty {
                                var word = Word(name: text)
                                
                                for attribute in Script.shared.words.reduce(into: Set<String>(), { x, y in
                                    if y.name == text {
                                        for attribute in y.attributes {
                                            if !x.contains(attribute) {
                                                x.insert(attribute)
                                            }
                                        }
                                    }
                                }) {
                                    word.attributes.append(attribute)
                                }
                                
                                Task {
                                    await self.talk(word: word, intensity: self.intensity, temperature: self.temperature, multiple: UIDevice.current.orientation.isLandscape, fallback: false)
                                }
                            }
                        }
                    } else if audioEngine.isRunning {
                        audioEngine.stop()
                        audioEngine.inputNode.removeTap(onBus: 0)
                        
                        withAnimation(.easeInOut(duration: 0.5)) {
                            self.volumeLevel = 0.0
                        }
                    }
                })
            } else {
                self.speechRecognizer = nil
                
                withAnimation(.easeInOut(duration: 0.5)) {
                    self.isRecording = false
                }
            }
        }
    }
    
    private func stopRecognize() {
        if self.speechRecognizer != nil {
            if let speechRecognitionTask = self.speechRecognitionTask {
                speechRecognitionTask.finish()
                self.speechRecognitionTask = nil
            }
            
            if let audioEngine = self.audioEngine {
                if audioEngine.isRunning {
                    audioEngine.stop()
                    audioEngine.inputNode.removeTap(onBus: 0)
                }
                
                self.audioEngine = nil
            }
            
            if let speechAudioBufferRecognitionRequest = self.speechAudioBufferRecognitionRequest {
                speechAudioBufferRecognitionRequest.endAudio()
                self.speechAudioBufferRecognitionRequest = nil
            }
            
            self.speechRecognizer = nil
            
            withAnimation(.easeInOut(duration: 0.5)) {
                self.isRecording = false
                self.volumeLevel = 0.0
            }
        }
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
    
    private func softmax(x: [Double], temperature: Double = 1.0) -> [Double] {
        var q = [Double]()
        var max = -Double.greatestFiniteMagnitude
        var sum = 0.0
        
        for z in x {
            if z > max {
                max = z
            }
        }
        
        for z in x {
            sum += exp((z - max) / temperature);
        }
        
        for z in x {
            q.append(exp((z - max) / temperature) / sum)
        }
        
        return q
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
}

struct Stage: UIViewRepresentable {
    @Binding var prompt: (previous: Word?, next: (String?, Word?, Bool, Set<Character>?, Double)?)
    @Binding var logs: [(id: UUID?, from: String?, to: String?, group: Double, content: String)]
    @Binding var resource: (old: String, new: String)
    @Binding var attributes: [String]
    @Binding var types: Int
    @Binding var labels: [String]
    @Binding var likes: (old: Int, new: [String: [Date]])
    @Binding var changing: Bool
    @Binding var loading: Bool
    @Binding var intensity: Double
    var temperature: Double
    var accent: UIColor
    var scale: Double
    var pause: Bool
    var mute: Bool
    @State var permissions: Set<String> = []
    
    init(prompt: Binding<(previous: Word?, next: (String?, Word?, Bool, Set<Character>?, Double)?)>, logs: Binding<[(id: UUID?, from: String?, to: String?, group: Double, content: String)]>, resource: Binding<(old: String, new: String)>, attributes: Binding<[String]>, types: Binding<Int>, labels: Binding<[String]>, likes: Binding<(old: Int, new: [String: [Date]])>, changing: Binding<Bool>, loading: Binding<Bool>, intensity: Binding<Double>, temperature: Double, accent: UIColor, scale: Double, pause: Bool, mute: Bool) {
        self._prompt = prompt
        self._logs = logs
        self._resource = resource
        self._attributes = attributes
        self._types = types
        self._labels = labels
        self._likes = likes
        self._changing = changing
        self._loading = loading
        self._intensity = intensity
        self.temperature = temperature
        self.accent = accent
        self.scale = scale
        self.pause = pause
        self.mute = mute
    }
    
    func makeUIView(context: Context) -> WallView {
        let wallView = WallView(frame: .zero)
        let agentView = AgentView(path: self.resource.old.isEmpty ? Double.random(in: 0..<1) < 0.5 ? "Milch" : "Merku" : self.resource.old, types: self.types, scale: self.scale)
        
        agentView.accent = self.accent
        agentView.mute = self.mute
        
        context.coordinator.uiView = wallView
        context.coordinator.scale = self.scale
        context.coordinator.update(agent: agentView)
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
            
            for i in 0..<agentView.characterViews.count {
                if i == 0 {
                    agentView.characterViews[i].refresh()
                }
                
                if let name = agentView.characterViews[i].name, let likes = Script.shared.likes[name] {
                    self.likes.new[name] = likes.reduce(into: [Date](), { x, y in
                        if y.id == nil {
                            x.append(y.timestamp)
                        }
                    })
                }
            }
            
            for (key, _) in self.likes.new {
                if !agentView.characterViews.contains(where: { $0.name == key }) {
                    self.likes.new.removeValue(forKey: key)
                }
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
                        
                        if let name = first.name, let value = self.likes.new[name], self.likes.old != value.count {
                            withAnimation(.linear(duration: 0.5)) {
                                self.likes.old = value.count
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
        var scale = 1.0
        var temperature = 1.0
        var pause = false
        private var parent: Stage
        private var snapshot: (name: String?, types: [String]) = (name: nil, types: [])
        private var lines: [(text: String, attributes: [(name: String, start: Int, end: Int)])] = []
        private var timestamp: Date? = nil
        private var intensities = [(Double, Date)]()
        
        init(_ parent: Stage) {
            self.parent = parent
        }
        
        func agentShouldIdle(_ agent: AgentView, by name: String) -> Bool {
            if self.parent.loading || self.pause || agent.characterViews.contains(where: { $0.name != name && !$0.balloonView!.isHidden }) || Double.random(in: 0.0..<1.0) < 0.5 {
                return true
            }
            
            if UIDevice.current.orientation.isLandscape || agent.characterViews.firstIndex(where: { $0.name == name }) == 0 {
                Task {
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
            
            return false
        }
        
        func agentDidRender(_ agent: AgentView, image: CGImage, by name: String) {
            if let uiView = self.uiView {
                var typeSet = Set<String>()
                
                for characterView in agent.characterViews {
                    if characterView.name == name {
                        let types = characterView.types.compactMap({ $0.value.1 ? $0.key : nil })
                        
                        if characterView.name != self.snapshot.name || !types.elementsEqual(self.snapshot.types) {
                            if characterView.fades.contains(where: { $0.value > 0.0 && $0.value < 1.0 }) {
                                let (i, _) = characterView.preview(animations: characterView.cachedAnimations, images: &characterView.cachedImages)
                                
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
                    
                    for animation in characterView.cachedAnimations {
                        if let type = animation.type, !typeSet.contains(type) {
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
        
        func agentDidRefresh(_ agent: AgentView, forcibly flag: Bool) {
            if let characterView = agent.characterViews.first {
                let language = characterView.language
                
                Task {
                    let words = await Task.detached {
                        let yesterday = Date(timeIntervalSinceNow: -60 * 60 * 24)
                        let epsilon = powl(10, -6)
                        var mean = 0.0
                        var data = [String: Double]()
                        var words: [Word] = []
                        
                        for (key, value) in Script.shared.scores {
                            if value.3 > yesterday && value.0 > epsilon {
                                mean += value.0
                                data[key] = value.0
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
                                for (key, value) in Script.shared.scores {
                                    if let x = data[key], (x - mean) / variance >= 2.0 && value.2 == language && !Script.shared.words.contains(where: { $0.name == key }) {
                                        var word = Word(name: key)
                                        
                                        for attribute in value.1 {
                                            word.attributes.append(attribute)
                                        }
                                        
                                        words.append(word)
                                    }
                                }
                            }
                        }
                        
                        return words
                    }.value
                    
                    if !words.isEmpty {
                        self.learn(words: words)
                    }
                }
            }
            
            self.update(agent: agent, force: flag)
        }
        
        func agentDidStop(_ agent: AgentView) {
            agent.change(path: self.parent.resource.new)
        }
        
        func agentDidChange(_ agent: AgentView) {
            self.parent.attributes.removeAll()
            self.parent.attributes.append(contentsOf: agent.attributes)
            self.parent.types = 0
            self.parent.intensity = 0.5
            self.intensities.removeAll()
            
            for characterView in agent.characterViews {
                if let name = characterView.name, let likes = Script.shared.likes[name] {
                    self.parent.likes.new[name] = likes.reduce(into: [Date](), { x, y in
                        if y.id == nil {
                            x.append(y.timestamp)
                        }
                    })
                }
            }
            
            for (key, _) in self.parent.likes.new {
                if !agent.characterViews.contains(where: { $0.name == key }) {
                    self.parent.likes.new.removeValue(forKey: key)
                }
            }
            
            withAnimation {
                self.parent.changing = false
            }
            
            self.update(agent: agent)
        }
        
        func agentDidLike(_ agent: AgentView, message: Message, with images: [[(url: URL?, x: Double, y: Double, width: Double, height: Double, opacity: Double, delay: Double)]]?) {
            var count = 0
            let nowDate = Date()
            let thresholdDate = Date(timeIntervalSinceNow: -60 * 60)
            
            for characterView in agent.characterViews {
                if let name = characterView.name, let likes = Script.shared.likes[name] {
                    let timestamps = likes.reduce(into: [Date](), { x, y in
                        if y.id == nil {
                            x.append(y.timestamp)
                        }
                    })
                    
                    count += timestamps.count
                    self.parent.likes.new[name] = timestamps
                }
            }
            
            if self.parent.logs.contains(where: { $0.id == message.id }) {
                self.intensities.append((1.0, nowDate))
            } else {
                self.intensities.append((0.0, nowDate))
            }
            
            for i in stride(from: self.intensities.count - 1, through: 0, by: -1) {
                if self.intensities[i].1 <= thresholdDate {
                    self.intensities.remove(at: i)
                }
            }
            
            self.parent.intensity = self.intensities.reduce(0.5, { $0 + $1.0 }) / Double(self.intensities.count + 1)
            self.update(agent: agent)
            
            if let images, let uiView = self.uiView {
                let particles = count
                
                if !images.isEmpty {
                    self.timestamp = nowDate
                }
                
                Task {
                    await uiView.reload(frames: images, particles: particles)
                }
            }
        }
        
        func wallCanSelect(_ wall: WallView, at index: Int) -> Bool {
            return index < self.lines.count && !self.lines[index].attributes.isEmpty
        }
        
        func wallDidSelect(_ wall: WallView, at index: Int) {
            if index < self.lines.count {
                let line = self.lines[index]
                var dictionary = [String: [String]]()
                
                for attribute in line.attributes {
                    let name = String(line.text[line.text.index(line.text.startIndex, offsetBy: attribute.start)..<line.text.index(line.text.startIndex, offsetBy: attribute.end)])
                    
                    if var attributes = dictionary[name] {
                        attributes.append(attribute.name)
                        dictionary[name] = attributes
                    } else {
                        dictionary[name] = [attribute.name]
                    }
                }
                
                if !dictionary.isEmpty {
                    let names = Array<String>(dictionary.keys)
                    let epsilon = powl(10, -6)
                    var minProbability = names.reduce(Double.greatestFiniteMagnitude, { x, y in
                        if let (score, _, _, _) = Script.shared.scores[y], score > epsilon && score < x {
                            return score
                        }
                        
                        return x
                    })
                    var probabilities = [Double]()
                    var letterSet: Set<Character> = []
                    
                    if minProbability == Double.greatestFiniteMagnitude {
                        minProbability = epsilon
                    }
                    
                    for name in names {
                        if let (score, _, _, _) = Script.shared.scores[name], score > epsilon {
                            probabilities.append(score)
                        } else {
                            probabilities.append(minProbability)
                        }
                        
                        for i in 0..<name.count {
                            let character = name[name.index(name.startIndex, offsetBy: i)]
                            
                            if !letterSet.contains(character) && !character.isNewline && !character.isWhitespace {
                                letterSet.insert(character)
                            }
                        }
                    }
                    
                    let sum = probabilities.reduce(0, { $0 + $1 })
                    
                    probabilities = probabilities.map({ $0 / sum })
                    
                    let name = names[min(self.choice(probabilities: probabilities), probabilities.count - 1)]
                    var word = Word(name: name)
                    
                    for attribute in dictionary[name]! {
                        word.attributes.append(attribute)
                    }
                    
                    if !Script.shared.words.contains(where: { $0.name == name }) {
                        self.learn(words: [word])
                    }
                    
                    Task {
                        await self.talk(word: word, intensity: self.parent.intensity, temperature: self.temperature, multiple: UIDevice.current.orientation.isLandscape)
                    }
                    
                    self.parent.prompt = (previous: word, next: (name, word, false, letterSet, CACurrentMediaTime()))
                }
            }
        }
        
        func update(agent: AgentView, force: Bool = false) {
            guard let uiView = self.uiView, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene else {
                return
            }
            
            let maxLines = Int(round(min(window.screen.bounds.width, window.screen.bounds.height) / ceil(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .body).pointSize * 1.5)))
            var language: String? = nil
            let yesterday = Date(timeIntervalSinceNow: -60 * 60 * 24)
            var lines: [(text: String, attributes: [(name: String, start: Int, end: Int)])] = []
            var baseLines: Int? = nil
            var isUpdated = false
            
            for preferredLanguage in Locale.preferredLanguages {
                if let languageCode = Locale(identifier: preferredLanguage).language.languageCode, let first = agent.characterViews.first, first.language == languageCode.identifier {
                    language = languageCode.identifier
                    
                    break
                }
            }
            
            var likes = Script.shared.likes.reduce(into: [(id: Int?, name: String, content: String, language: String?, attributes: [(name: String, start: Int, end: Int)], timestamp: Date)]()) { x, y in
                for like in y.value {
                    if like.language == language {
                        x.append(like)
                    }
                }
            }
            
            likes.sort { $0.timestamp > $1.timestamp }
            
            for i in stride(from: likes.count - 1, through: 0, by: -1) {
                if likes[i].timestamp > yesterday {
                    var attributes: [(name: String, start: Int, end: Int)] = []
                    var count = 0
                    var offset = 0
                    
                    for attribute in likes[i].attributes {
                        if !attributes.contains(where: { $0.start == attribute.start && $0.end == attribute.end }) {
                            attributes.append((name: attribute.name, start: attribute.start, end: attribute.end))
                        }
                    }
                    
                    while count < likes[i].content.count {
                        let character = likes[i].content[likes[i].content.index(likes[i].content.startIndex, offsetBy: count)]
                        
                        if character.isNewline {
                            var fixedAttributes: [(name: String, start: Int, end: Int)] = []
                            
                            for attribute in attributes {
                                if attribute.start - offset >= 0 && attribute.end <= count {
                                    fixedAttributes.append((name: attribute.name, start: attribute.start - offset, end: attribute.end - offset))
                                }
                            }
                            
                            lines.insert((text: String(likes[i].content[likes[i].content.index(likes[i].content.startIndex, offsetBy: offset)..<likes[i].content.index(likes[i].content.startIndex, offsetBy: count)]).uppercased(), attributes: fixedAttributes), at: 0)
                            offset = count + 1
                        }
                        
                        count += 1
                    }
                    
                    if count > offset {
                        var fixedAttributes: [(name: String, start: Int, end: Int)] = []
                        
                        for attribute in attributes {
                            if attribute.start - offset >= 0 {
                                fixedAttributes.append((name: attribute.name, start: attribute.start - offset, end: attribute.end - offset))
                            }
                        }
                        
                        lines.insert((text: String(likes[i].content[likes[i].content.index(likes[i].content.startIndex, offsetBy: offset)..<likes[i].content.index(likes[i].content.startIndex, offsetBy: count)]).uppercased(), attributes: fixedAttributes), at: 0)
                    }
                    
                    likes.remove(at: i)
                }
            }
            
            if lines.count < maxLines {
                baseLines = lines.count
                
                for i in 0..<min(likes.count, maxLines - lines.count) {
                    let like = likes[i]
                    var attributes: [(name: String, start: Int, end: Int)] = []
                    var count = 0
                    var offset = 0
                    
                    for attribute in like.attributes {
                        if !attributes.contains(where: { $0.start == attribute.start && $0.end == attribute.end }) {
                            attributes.append((name: attribute.name, start: attribute.start, end: attribute.end))
                        }
                    }
                    
                    while count < like.content.count {
                        let character = like.content[like.content.index(like.content.startIndex, offsetBy: count)]
                        
                        if character.isNewline {
                            var fixedAttributes: [(name: String, start: Int, end: Int)] = []
                            
                            for attribute in attributes {
                                if attribute.start - offset >= 0 && attribute.end <= count {
                                    fixedAttributes.append((name: attribute.name, start: attribute.start - offset, end: attribute.end - offset))
                                }
                            }
                            
                            lines.append((text: String(like.content[like.content.index(like.content.startIndex, offsetBy: offset)..<like.content.index(like.content.startIndex, offsetBy: count)]).uppercased(), attributes: fixedAttributes))
                            offset = count + 1
                        }
                        
                        count += 1
                    }
                    
                    if count > offset {
                        var fixedAttributes: [(name: String, start: Int, end: Int)] = []
                        
                        for attribute in attributes {
                            if attribute.start - offset >= 0 {
                                fixedAttributes.append((name: attribute.name, start: attribute.start - offset, end: attribute.end - offset))
                            }
                        }
                        
                        lines.append((text: String(like.content[like.content.index(like.content.startIndex, offsetBy: offset)..<like.content.index(like.content.startIndex, offsetBy: count)]).uppercased(), attributes: fixedAttributes))
                    }
                }
            }
            
            if lines.count > maxLines {
                lines.removeSubrange(maxLines..<lines.count)
            }
            
            if lines.count == self.lines.count {
                for i in 0..<lines.count {
                    if !isUpdated {
                        if lines[i].text == self.lines[i].text && lines[i].attributes.count == self.lines[i].attributes.count {
                            for j in 0..<lines[i].attributes.count {
                                if lines[i].attributes[j].start != self.lines[i].attributes[j].start && lines[i].attributes[j].end != self.lines[i].attributes[j].end {
                                    isUpdated = true
                                    
                                    break
                                }
                            }
                        } else {
                            isUpdated = true
                        }
                    }
                    
                    self.lines[i] = lines[i]
                }
            } else if lines.count < self.lines.count {
                for i in 0..<lines.count {
                    self.lines[i] = lines[i]
                }
                
                self.lines.removeSubrange(lines.count..<self.lines.count)
                isUpdated = true
            } else {
                for i in 0..<self.lines.count {
                    self.lines[i] = lines[i]
                }
                
                for i in self.lines.count..<lines.count {
                    self.lines.append(lines[i])
                }
                
                isUpdated = true
            }
            
            if force || isUpdated {
                if let baseLines, baseLines < lines.count {
                    let index = max(baseLines, maxLines / 2)
                    
                    if index < lines.count {
                        let start = Int.random(in: index...lines.count)
                        
                        if start < lines.count {
                            lines.removeSubrange(start..<lines.count)
                        }
                    }
                }
                
                uiView.reload(lines: lines.reduce(into: [], { x, y in
                    var attributes = [(start: Int, end: Int)]()
                    
                    for attribute in y.attributes {
                        attributes.append((start: attribute.start, end: attribute.end))
                    }
                    
                    x.append((text: y.text, attributes: attributes))
                }))
            }
        }
        
        private func learn(words: [Word]) {
            Task.detached {
                let encoder = JSONEncoder()
                let image = UIImage(systemName: "book", withConfiguration: UIImage.SymbolConfiguration(font: .systemFont(ofSize: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .caption1).pointSize, weight: .bold)))!
                
                await MainActor.run {
                    for word in words {
                        withAnimation {
                            Script.shared.words.append(word)
                        }
                    }
                }
                
                if let data = try? encoder.encode(Script.shared.words) {
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
                
                await MainActor.run { [weak self] in
                    if let uiView = self?.uiView {
                        for view in uiView.subviews {
                            if let agentView = view as? AgentView {
                                if let characterView = agentView.characterViews.first {
                                    for word in words {
                                        agentView.notify(characterView: characterView, image: image, text: word.name, duration: 5.0)
                                    }
                                }
                                
                                break
                            }
                        }
                    }
                }
            }
        }
        
        @MainActor private func talk(word: Word, intensity: Double, temperature: Double, multiple: Bool) {
            Task {
                var queue = Script.shared.characters
                
                if let first = queue.first {
                    var logs = [(id: UUID?, from: String?, to: String?, group: Double, content: String)]()
                    var generateRequired: Bool
                    var input: String
                    var time: Double
                    var sequences = [(String, UUID?, Sequence)]()
                    
                    if multiple {
                        queue.removeFirst()
                        
                        if let last = self.parent.logs.last {
                            var isContinuous = false
                            
                            for log in self.parent.logs {
                                if log.group == last.group {
                                    if log.from == nil && log.content == word.name {
                                        isContinuous = true
                                    }
                                    
                                    logs.append(log)
                                }
                            }
                            
                            if isContinuous {
                                generateRequired = true
                                input = String()
                                time = last.group
                            } else {
                                logs.removeAll()
                                generateRequired = word.attributes.isEmpty ? true : Double.random(in: 0..<1) < intensity
                                input = word.name
                                time = CACurrentMediaTime()
                            }
                        } else {
                            generateRequired = word.attributes.isEmpty ? true : Double.random(in: 0..<1) < intensity
                            input = word.name
                            time = CACurrentMediaTime()
                        }
                    } else {
                        queue.removeAll()
                        
                        if let last = self.parent.logs.last, self.parent.logs.contains(where: { $0.from == nil && $0.group == last.group && $0.content == word.name }) {
                            generateRequired = true
                            input = String()
                            time = last.group
                        } else {
                            generateRequired = word.attributes.isEmpty ? true : Double.random(in: 0..<1) < intensity
                            input = word.name
                            time = CACurrentMediaTime()
                        }
                    }
                    
                    if let prompt = first.prompt, generateRequired {
                        withAnimation(.easeOut(duration: 0.5)) {
                            self.parent.loading = true
                        }
                        
                        var messages: [[String: Any]] = [["role": "system", "content": prompt]]
                        var i = logs.count - 1
                        
                        while i > 0 {
                            if let from = logs[i].from, from != first.name && logs[i - 1].from == first.name {
                                messages.insert(["role": "user", "content": logs[i].content], at: 1)
                                messages.insert(["role": "assistant", "content": logs[i - 1].content], at: 1)
                                
                                i -= 2
                            } else {
                                i -= 1
                            }
                        }
                        
                        if messages.count == 1 {
                            var i = self.parent.logs.count - 1
                            
                            while i > 0 {
                                if self.parent.logs[i].from == first.name && self.parent.logs[i].to == nil && self.parent.logs[i - 1].from == nil && self.parent.logs[i - 1].to == first.name {
                                    messages.insert(["role": "assistant", "content": self.parent.logs[i].content], at: 1)
                                    messages.insert(["role": "user", "content": self.parent.logs[i - 1].content], at: 1)
                                    
                                    i -= 2
                                } else {
                                    i -= 1
                                }
                            }
                            
                            messages.append(["role": "user", "content": input])
                        }
                        
                        if let (content, state) = await self.generate(messages: messages, temperature: temperature) {
                            var text = String(content)
                            let sequence = Sequence(name: "Activate", state: nil)
                            let id = UUID()
                            var inlines = [(text: String, attributes: [String]?)]()
                            
                            while !text.isEmpty {
                                if !word.attributes.isEmpty, let range = text.range(of: word.name) {
                                    if text.startIndex != range.lowerBound {
                                        inlines.append((text: String(text[text.startIndex..<range.lowerBound]), attributes: nil))
                                    }
                                    
                                    inlines.append((text: String(text[range.lowerBound..<range.upperBound]), attributes: word.attributes))
                                    text = String(text[range.upperBound..<text.endIndex])
                                } else {
                                    inlines.append((text: text, attributes: nil))
                                    text.removeAll()
                                }
                            }
                            
                            sequence.append(Message(id: id, inlines: inlines))
                            sequence.append(Sequence(name: "Emote", state: state ?? String()))
                            sequences.append((first.name, id, sequence))
                            
                            while !queue.isEmpty {
                                let character = queue.removeFirst()
                                
                                if let prompt = character.prompt {
                                    var messages: [[String: Any]] = [["role": "system", "content": prompt], ["role": "user", "content": content]]
                                    var i = logs.count - 1
                                    
                                    while i > 0 {
                                        if logs[i].from == character.name {
                                            messages.insert(["role": "assistant", "content": logs[i].content], at: 1)
                                            messages.insert(["role": "user", "content": logs[i - 1].content], at: 1)
                                            
                                            i -= 2
                                        } else {
                                            i -= 1
                                        }
                                    }
                                    
                                    if let (content, state) = await self.generate(messages: messages, temperature: temperature) {
                                        var text = String(content)
                                        let sequence = Sequence(name: "Activate", state: nil)
                                        let id = UUID()
                                        var inlines = [(text: String, attributes: [String]?)]()
                                        
                                        while !text.isEmpty {
                                            if !word.attributes.isEmpty, let range = text.range(of: word.name) {
                                                if text.startIndex != range.lowerBound {
                                                    inlines.append((text: String(text[text.startIndex..<range.lowerBound]), attributes: nil))
                                                }
                                                
                                                inlines.append((text: String(text[range.lowerBound..<range.upperBound]), attributes: word.attributes))
                                                text = String(text[range.upperBound..<text.endIndex])
                                            } else {
                                                inlines.append((text: text, attributes: nil))
                                                text.removeAll()
                                            }
                                        }
                                        
                                        sequence.append(Message(id: id, inlines: inlines))
                                        sequence.append(Sequence(name: "Emote", state: state ?? String()))
                                        sequences.append((character.name, id, sequence))
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
                    } else {
                        queue.removeAll()
                    }
                    
                    if sequences.isEmpty {
                        await Script.shared.run(name: first.name, sequences: Script.shared.characters.reduce(into: [], { x, y in
                            if y.name == first.name {
                                for sequence in y.sequences {
                                    if sequence.name == "Activate" {
                                        x.append(sequence)
                                    }
                                }
                            }
                        }), words: [word], temperature: temperature) { x in
                            if !x.isEmpty {
                                var y = x
                                var content = [String]()
                                
                                for sequence in x {
                                    for obj in sequence {
                                        if let message = obj as? Message {
                                            content.append(message.content)
                                        }
                                    }
                                }
                                
                                y.append(Sequence(name: String()))
                                
                                self.parent.logs.append((id: nil, from: nil, to: first.name, group: time, content: word.name))
                                self.parent.logs.append((id: nil, from: first.name, to: nil, group: time, content: content.joined(separator: "\n")))
                                
                                return y
                            }
                            
                            return x
                        }
                    } else {
                        for i in 0..<sequences.count {
                            await Script.shared.run(name: sequences[i].0, sequences: [sequences[i].2], words: []) { x in
                                var y = x
                                var content = [String]()
                                
                                for sequence in x {
                                    for obj in sequence {
                                        if let message = obj as? Message {
                                            content.append(message.content)
                                        }
                                    }
                                }
                                
                                y.append(Sequence(name: String()))
                                
                                if i > 0 {
                                    self.parent.logs.append((id: sequences[i].1, from: sequences[i].0, to: sequences[0].0, group: time, content: content.joined(separator: "\n")))
                                } else {
                                    if !input.isEmpty {
                                        self.parent.logs.append((id: nil, from: nil, to: sequences[i].0, group: time, content: word.name))
                                    }
                                    
                                    self.parent.logs.append((id: sequences[i].1, from: sequences[i].0, to: nil, group: time, content: content.joined(separator: "\n")))
                                }
                                
                                return y
                            }
                        }
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
            }
        }
        
        private func generate(messages: [[String: Any]], temperature: Double) async -> (String, String?)? {
            if let data = try? JSONSerialization.data(withJSONObject: messages) {
                var request = URLRequest(url: URL(string: "https://milchchan.com/api/generate?temperature=\(String(format: "%.1f", temperature))".addingPercentEncoding(withAllowedCharacters: .urlQueryAllowed)!)!)
                
                request.httpMethod = "POST"
                request.setValue("application/json", forHTTPHeaderField: "Content-Type")
                request.httpBody = data
                
                if let (data, response) = try? await URLSession.shared.data(for: request), let httpResponse = response as? HTTPURLResponse, (200...299).contains(httpResponse.statusCode), httpResponse.mimeType == "application/json", let jsonObject = try? JSONSerialization.jsonObject(with: data), let jsonRoot = jsonObject as? [String: Any], let content = jsonRoot["content"] as? String {
                    var state: String? = nil
                    
                    if let states = jsonRoot["states"] as? [String: Any] {
                        var max = 0.0
                        
                        for (key, object) in states {
                            if let value = object as? Double, value > max {
                                state = key
                                max = value
                            }
                        }
                    }
                    
                    return (content, state)
                }
            }
            
            return nil
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
    }
}

struct Prompt: UIViewRepresentable {
    let input: (String?, Word?, Bool, Set<Character>?, Double)?
    let accent: UIColor
    let font: UIFont
    
    func makeUIView(context: Context) -> PromptView {
        return PromptView(frame: .zero)
    }
    
    func updateUIView(_ uiView: PromptView, context: Context) {
        uiView.accent = self.accent
        uiView.font = self.font
        
        if let input = self.input, let text = input.0 {
            if context.coordinator.timestamp != input.4 {
                if !input.2 && uiView.isScrambled {
                    uiView.isScrambled = false
                } else  {
                    uiView.isScrambled = input.2
                    uiView.scrambleLetters = input.3
                    uiView.reload(text: text)
                }
                
                context.coordinator.timestamp = input.4
            }
        } else {
            uiView.reload(text: nil)
        }
    }
    
    func makeCoordinator() -> Coordinator {
        return Coordinator()
    }
    
    class Coordinator: NSObject {
        var timestamp = 0.0
    }
}

struct Activity: View {
    private let accent: UIColor
    @Binding private var logs: [(id: UUID?, from: String?, to: String?, group: Double, content: String)]
    @Environment(\.dismiss) private var dismiss
    @Namespace private var topID
    @State private var names: [(String?)]
    @State private var messages: [String]
    @State private var isDefault = true
    private var stats: [Int] = []
    private var mean: Double = 0.0
    private var variance: Double = 0.0
    private var achievements: [String] = []
    private var remains: [Int?] = []
    
    var body: some View {
        NavigationStack {
            ScrollViewReader { proxy in
                List {
                    EmptyView()
                        .id(self.topID)
                    
                    if self.isDefault {
                        self.makeStats()
                        
                        if !self.achievements.isEmpty {
                            self.makeAchievements()
                        }
                    } else {
                        self.makeLogs()
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
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .font(.headline)
                            .fontWeight(.semibold)
                            .lineLimit(1)
                            .textCase(.uppercase)
                    }
                    ToolbarItem(placement: .navigationBarLeading) {
                        Button(action: {
                            dismiss()
                        }) {
                            Circle()
                                .fill(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                }))
                                .frame(
                                    width: 32.0,
                                    height: 32.0
                                )
                                .overlay(Image(systemName: "xmark")
                                    .frame(
                                        alignment: .center
                                    )
                                        .background(.clear)
                                        .foregroundColor(Color(UIColor {
                                            $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                        }))
                                            .font(
                                                .system(size: 8.0)
                                            )
                                                .bold())
                        }
                        .background(.clear)
                    }
                    ToolbarItem(placement: .navigationBarTrailing) {
                        Button(action: {
                            withAnimation {
                                proxy.scrollTo(self.topID, anchor: .bottom)
                                self.isDefault.toggle()
                            }
                        }) {
                            Circle()
                                .fill(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                }))
                                .frame(
                                    width: 32.0,
                                    height: 32.0
                                )
                                .overlay(Image(systemName: "arrow.left.arrow.right")
                                    .frame(
                                        alignment: .center
                                    )
                                        .background(.clear)
                                        .foregroundColor(Color(UIColor {
                                            $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                        }))
                                            .font(
                                                .system(size: 8.0)
                                            )
                                                .bold())
                        }
                        .background(.clear)
                    }
                }
                .transition(.opacity)
            }
        }
    }
    
    init(accent: UIColor, data: [(name: String, sequences: [Sequence], likes: [Date]?)], logs: Binding<[(id: UUID?, from: String?, to: String?, group: Double, content: String)]>) {
        var names = [String?]()
        var messages = [String]()
        let maxDays = 6
        let nowDateComponents = Calendar.current.dateComponents([.calendar, .timeZone, .era, .year, .month, .day], from: Date())
        
        for log in logs.wrappedValue {
            names.append(log.from)
            messages.append(log.content)
        }
        
        self.accent = accent
        self._logs = logs
        self._names = State(initialValue: names)
        self._messages = State(initialValue: messages)
        
        for i in stride(from: -maxDays, through: 0, by: 1) {
            let dateComponents = DateComponents(calendar: nowDateComponents.calendar, timeZone: nowDateComponents.timeZone, era: nowDateComponents.era, year: nowDateComponents.year, month: nowDateComponents.month, day: nowDateComponents.day! + i, hour: 0, minute: 0, second: 0, nanosecond: 0)
            var count = 0
            
            for character in data {
                if let likes = character.likes {
                    for date in likes {
                        let dc = Calendar.current.dateComponents([.year, .month, .day], from: date)
                        
                        if dateComponents.year == dc.year && dateComponents.month == dc.month && dateComponents.day == dc.day {
                            count += 1
                        }
                    }
                }
            }
            
            self.stats.append(count)
        }
        
        self.mean = self.mean(data: self.stats)
        self.variance = self.variance(data: self.stats, mean: self.mean)
        
        for character in data {
            let likes = character.likes == nil ? 0 : character.likes!.count
            var available = 0
            var max = 0
            var lockedAchievements: [String: Int] = [:]
            var unlockableAchievementSet: Set<String> = []
            var tempAchievements: [(name: String, count: Int?)] = []
            
            for sequence in character.sequences {
                var isLocked = true
                var requiredLikes = 0
                
                if let pattern = sequence.state, let regex = try? Regex(pattern) {
                    for i in 0...likes + 10 {
                        if let match = "\(i)".firstMatch(of: regex), !match.output.isEmpty {
                            if i <= likes {
                                available += 1
                                isLocked = false
                            } else {
                                requiredLikes = i - likes
                            }
                            
                            break
                        }
                    }
                }
                
                for i in 0..<sequence.count {
                    if let s1 = sequence[i] as? Sequence {
                        if let name = s1.name, s1.state == nil && !s1.isEmpty {
                            for j in stride(from: i + 1, to: sequence.count, by: 1) {
                                if let s2 = sequence[j] as? Sequence {
                                    var isAvailable = false
                                    
                                    if s2.isEmpty {
                                        if s1.name == s2.name && s2.state == nil {
                                            isAvailable = true
                                        }
                                    } else {
                                        var queue: [Sequence] = []
                                        
                                        for obj in s2 {
                                            if let s3 = obj as? Sequence {
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
                                                for obj in s {
                                                    if let s3 = obj as? Sequence {
                                                        queue.append(s3)
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    
                                    if isAvailable {
                                        if isLocked {
                                            if let count = lockedAchievements[name] {
                                                if requiredLikes > count {
                                                    lockedAchievements[name] = requiredLikes
                                                }
                                            } else {
                                                lockedAchievements[name] = requiredLikes
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
                self.achievements.append(name)
                self.remains.append(count)
            }
        }
    }
    
    private func makeStats() -> some View {
        return Section(header: Text("Stats")
            .foregroundColor(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
            }))
                .fontWeight(.semibold)
                .lineLimit(1)
                .textCase(.uppercase)) {
                    Chart {
                        let today = Calendar.current.startOfDay(for: Date())
                        
                        ForEach(Array(self.stats.enumerated()), id: \.element) { (index, item) in
                            BarMark(
                                x: .value("Time", Calendar.current.date(byAdding: .day, value: -self.stats.count + 1 + index, to: today)!),
                                y: .value("Likes", item),
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
                    .aspectRatio(UIDevice.current.userInterfaceIdiom == .phone ? 1.0 : 2.0, contentMode: .fit)
                    .listRowBackground(Color(UIColor {
                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                    }))
                    .transition(.opacity.animation(.linear))
                    HStack(alignment: .center, spacing: 16.0) {
                        Text("Mean")
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                        Spacer()
                        Text(String(format: "%.1f", self.mean))
                            .foregroundColor(Color(uiColor: self.accent))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .lineLimit(1)
                    }
                    .listRowBackground(Color(UIColor {
                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                    }))
                    .transition(.opacity.animation(.linear))
                    HStack(alignment: .center, spacing: 16.0) {
                        Text("Variance")
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                        Spacer()
                        Text(String(format: "%.1f", self.variance))
                            .foregroundColor(Color(uiColor: self.accent))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .lineLimit(1)
                    }
                    .listRowBackground(Color(UIColor {
                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                    }))
                    .transition(.opacity.animation(.linear))
                }
    }
    
    private func makeAchievements() -> some View {
        return Section(header: Text("Achievements")
            .foregroundColor(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
            }))
                .fontWeight(.semibold)
                .lineLimit(1)
                .textCase(.uppercase)) {
                    HStack(alignment: .center, spacing: 16.0) {
                        Text("Overall")
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                        Spacer()
                        Text(String(format: "%.1f%%", Double(self.achievements.enumerated().reduce(0, { self.remains[$1.offset] == nil ? $0 + 1 : $0 })) / Double(self.achievements.count)))
                            .foregroundColor(Color(uiColor: self.accent))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .lineLimit(1)
                    }
                    .listRowBackground(Color(UIColor {
                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                    }))
                    .transition(.opacity.animation(.linear))
                    ForEach(Array(self.achievements.enumerated()), id: \.element) { (index, item) in
                        HStack(alignment: .center, spacing: 16.0) {
                            Text(item)
                                .foregroundColor(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                }))
                                .font(.subheadline)
                                .fontWeight(.semibold)
                            Spacer()
                            
                            if let count = self.remains[index] {
                                HStack(alignment: .center, spacing: 8.0) {
                                    Image(systemName: "lock")
                                        .frame(
                                            width: 16.0,
                                            height: 16.0,
                                            alignment: .center
                                        )
                                        .background(.clear)
                                        .foregroundColor(Color(uiColor: self.accent))
                                        .font(
                                            .system(size: 16.0)
                                        )
                                        .bold()
                                    
                                    if count > 0 {
                                        Text(String(format: "%ld", -count))
                                            .foregroundColor(Color(uiColor: self.accent))
                                            .font(.subheadline)
                                            .fontWeight(.semibold)
                                            .lineLimit(1)
                                    } else {
                                        HStack(alignment: .center, spacing: 8.0) {
                                            Image(systemName: "greaterthan")
                                                .background(.clear)
                                                .foregroundColor(Color(uiColor: self.accent))
                                                .font(
                                                    .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                                                )
                                            Text(String(format: "%ld", 10))
                                                .foregroundColor(Color(uiColor: self.accent))
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
                                    .foregroundColor(Color(uiColor: self.accent))
                                    .font(
                                        .system(size: 16.0)
                                    )
                                    .bold()
                            }
                        }
                        .listRowBackground(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                        }))
                    }
                    .transition(.opacity.animation(.linear))
                }
    }
    
    private func makeLogs() -> some View {
        return Section(header: Text("Logs")
            .foregroundColor(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
            }))
                .fontWeight(.semibold)
                .lineLimit(1)
                .textCase(.uppercase)) {
                    if self.messages.isEmpty {
                        Text("None")
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .frame(
                                maxWidth: .infinity,
                                alignment: .center
                            )
                            .listRowBackground(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                            }))
                            .transition(.opacity.animation(.linear))
                    } else {
                        ForEach(Array(self.messages.reversed().enumerated()), id: \.element) { (index, message) in
                            if let name = self.names[self.names.count - 1 - index] {
                                VStack(alignment: .leading, spacing: 8.0) {
                                    HStack(alignment: .center, spacing: 8.0) {
                                        Text(name)
                                            .foregroundColor(Color(uiColor: self.accent))
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
                                            .foregroundColor(Color(uiColor: self.accent))
                                            .font(
                                                .system(size: 16.0)
                                            )
                                            .bold()
                                    }
                                    Text(message)
                                        .foregroundColor(Color(UIColor {
                                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                        }))
                                        .font(.subheadline)
                                        .fontWeight(.semibold)
                                        .frame(
                                            maxWidth: .infinity,
                                            alignment: .leading
                                        )
                                        .multilineTextAlignment(.leading)
                                }
                                .listRowBackground(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                }))
                            } else {
                                HStack(alignment: .center, spacing: 8.0) {
                                    Text(message)
                                        .foregroundColor(Color(uiColor: self.accent))
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
                                        .foregroundColor(Color(uiColor: self.accent))
                                        .font(
                                            .system(size: 16.0)
                                        )
                                        .bold()
                                }
                                .listRowBackground(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                }))
                            }
                        }
                        .transition(.opacity.animation(.linear))
                        Button(action: {
                            withAnimation {
                                self.names.removeAll()
                                self.messages.removeAll()
                            }
                            
                            self.logs.removeAll()
                        }) {
                            Text("Reset")
                                .foregroundColor(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                }))
                                .font(.subheadline)
                                .fontWeight(.semibold)
                                .frame(
                                    maxWidth: .infinity,
                                    alignment: .center
                                )
                                .contentShape(Rectangle())
                        }
                        .buttonStyle(PlainButtonStyle())
                        .listRowBackground(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                        }))
                        .transition(.opacity.animation(.linear))
                    }
                }
    }
    
    private func mean(data: [Int]) -> Double {
        var sum = 0.0
        
        for x in data {
            sum += Double(x)
        }
        
        return sum / Double(data.count)
    }
    
    private func variance(data: [Int], mean: Double) -> Double {
        var sum = 0.0
        
        for x in data {
            sum += (Double(x) - mean) * (Double(x) - mean)
        }
        
        return sum / Double(data.count)
    }
}

struct Dictionary: View {
    let accent: UIColor
    @Binding var type: String?
    @Binding var words: [Word]
    let attributes: [String]
    @Environment(\.dismiss) private var dismiss
    @FocusState private var inputFocus: Bool
    @Namespace private var topID
    @State private var isEditing = false
    @State private var isSubmittable = false
    @State private var isRecording = false
    @State private var isCapturing = false
    @State private var input = String()
    @State private var selectedAttributes: Set<String> = []
    @State private var path: [Word] = []
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
                                .foregroundColor(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                }))
                                .font(.headline)
                                .fontWeight(.semibold)
                                .lineLimit(1)
                                .textCase(.uppercase)
                                .opacity(self.path.isEmpty ? 1.0 : 0.0)
                            
                            if !self.path.isEmpty {
                                Text(self.path[0].name)
                                    .foregroundColor(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
                                    .font(.headline)
                                    .fontWeight(.semibold)
                                    .lineLimit(1)
                                    .textCase(.uppercase)
                                    .transition(.opacity)
                            }
                        }
                        .frame(maxWidth: .infinity)
                    }
                    ToolbarItem(placement: .navigationBarLeading) {
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
                                Circle()
                                    .fill(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
                                    .frame(
                                        width: 32.0,
                                        height: 32.0
                                    )
                                    .overlay(Image(systemName: "xmark")
                                        .frame(
                                            alignment: .center
                                        )
                                            .background(.clear)
                                            .foregroundColor(Color(UIColor {
                                                $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                            }))
                                                .font(.system(size: 8.0))
                                                .bold())
                                    .transition(.opacity)
                            } else {
                                Circle()
                                    .fill(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
                                    .frame(
                                        width: 32.0,
                                        height: 32.0
                                    )
                                    .overlay(Image(systemName: "arrow.backward")
                                        .frame(
                                            alignment: .center
                                        )
                                            .background(.clear)
                                            .foregroundColor(Color(UIColor {
                                                $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                            }))
                                                .font(.system(size: 8.0))
                                                .bold())
                                    .transition(.opacity)
                            }
                        }
                        .background(.clear)
                    }
                    ToolbarItem(placement: .navigationBarTrailing) {
                        Button(action: {
                            if self.path.isEmpty {
                                if self.inputFocus {
                                    self.inputFocus = false
                                    
                                    if self.isEditing {
                                        withAnimation(.easeInOut(duration: 0.5)) {
                                            self.isEditing = false
                                        }
                                    }
                                } else {
                                    withAnimation(.easeInOut(duration: 0.5)) {
                                        self.isEditing.toggle()
                                    }
                                }
                            } else if let index = self.words.firstIndex(where: { $0.id == self.path[0].id }) {
                                for attribute in self.attributes {
                                    if let i = self.words[index].attributes.firstIndex(where: { $0 == attribute }) {
                                        withAnimation {
                                            _ = self.words[index].attributes.remove(at: i)
                                        }
                                    }
                                }
                                
                                self.save(words: self.words)
                            }
                        }) {
                            if self.path.isEmpty {
                                if self.isEditing || self.inputFocus {
                                    Circle()
                                        .fill(Color(UIColor {
                                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                        }))
                                        .frame(
                                            width: 32.0,
                                            height: 32.0
                                        )
                                        .overlay(Image(systemName: "checkmark")
                                            .frame(
                                                alignment: .center
                                            )
                                                .background(.clear)
                                                .foregroundColor(Color(UIColor {
                                                    $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                                }))
                                                    .font(.system(size: 8.0))
                                                    .bold())
                                        .transition(.opacity)
                                } else {
                                    Circle()
                                        .fill(Color(UIColor {
                                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                        }))
                                        .frame(
                                            width: 32.0,
                                            height: 32.0
                                        )
                                        .overlay(Image(systemName: "pencil")
                                            .frame(
                                                alignment: .center
                                            )
                                                .background(.clear)
                                                .foregroundColor(Color(UIColor {
                                                    $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                                }))
                                                    .font(.system(size: 8.0))
                                                    .bold())
                                        .transition(.opacity)
                                }
                            } else {
                                Circle()
                                    .fill(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
                                    .frame(
                                        width: 32.0,
                                        height: 32.0
                                    )
                                    .overlay(Image(systemName: "gobackward")
                                        .frame(
                                            alignment: .center
                                        )
                                            .background(.clear)
                                            .foregroundColor(Color(UIColor {
                                                $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                            }))
                                                .font(.system(size: 8.0))
                                                .bold())
                                    .transition(.opacity)
                            }
                        }
                        .background(.clear)
                    }
                }
                .sheet(isPresented: self.$isCapturing, content: {
                    Camera(text: self.$input)
                        .presentationDetents([.medium, .large])
                })
                .onChange(of: self.type) {
                    if let type = self.type {
                        self.input = type
                        self.type = nil
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
    
    init(accent: UIColor, type: Binding<String?>, words: Binding<[Word]>, attributes: [String]) {
        self.accent = accent
        self._type = type
        self._words = words
        self.attributes = attributes
    }
    
    private func makeNew() -> some View {
        Section(header: Text("New")
            .foregroundColor(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
            }))
                .fontWeight(.semibold)
                .lineLimit(1)
                .textCase(.uppercase)) {
                    HStack(alignment: .center, spacing: 16.0) {
                        TextField("Word", text: self.$input)
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .submitLabel(.done)
                            .focused(self.$inputFocus)
                            .textInputAutocapitalization(.never)
                            .disableAutocorrection(true)
                            .background(.clear)
                            .onChange(of: self.input) {
                                if self.input.isEmpty {
                                    withAnimation(.easeIn(duration: 0.5)) {
                                        self.isSubmittable = false
                                    }
                                } else {
                                    withAnimation(.easeOut(duration: 0.5)) {
                                        self.isSubmittable = true
                                    }
                                }
                            }
                            .onChange(of: self.inputFocus) {
                                if self.inputFocus && self.isRecording {
                                    self.stopRecognize()
                                }
                            }
                            .onChange(of: self.isRecording) {
                                if self.isRecording && self.inputFocus {
                                    self.inputFocus = false
                                }
                            }
                            .onChange(of: self.isCapturing) {
                                if self.isCapturing && self.isRecording {
                                    self.stopRecognize()
                                }
                            }
                            .onSubmit {
                                if self.input.isEmpty {
                                    self.inputFocus = false
                                } else {
                                    Task {
                                        let input = self.input
                                        
                                        await Task.detached {
                                            await MainActor.run {
                                                self.inputFocus = false
                                                self.input = String()
                                            }
                                        }.value
                                        
                                        await Task.detached {
                                            await MainActor.run {
                                                withAnimation {
                                                    self.words.append(Word(name: input))
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
                                    .foregroundColor(Color(uiColor: self.accent))
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
                                    .foregroundColor(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
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
                                    .foregroundColor(Color(uiColor: self.accent))
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
                                    .foregroundColor(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
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
                                        if self.isRecording {
                                            self.stopRecognize()
                                        }
                                        
                                        self.inputFocus = false
                                        self.input = String()
                                    }
                                }.value
                                
                                await Task.detached {
                                    await MainActor.run {
                                        withAnimation {
                                            self.words.append(Word(name: input))
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
                                .foregroundColor(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                }))
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
                    .listRowBackground(Color(UIColor {
                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                    }))
                    .transition(.opacity.animation(.linear))
                }
    }
    
    private func makeWords(proxy: ScrollViewProxy) -> some View {
        return Section(header: Text("Words")
            .foregroundColor(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
            }))
                .fontWeight(.semibold)
                .lineLimit(1)
                .textCase(.uppercase)) {
                    if self.words.isEmpty {
                        Text("None")
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .frame(
                                maxWidth: .infinity,
                                alignment: .center
                            )
                            .listRowBackground(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                            }))
                            .transition(.opacity.animation(.linear))
                    } else {
                        ForEach(Array(self.words.reversed().enumerated()), id: \.element) { (index, word) in
                            if self.attributes.isEmpty {
                                Text(word.name)
                                    .foregroundColor(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
                                    .font(.subheadline)
                                    .fontWeight(.semibold)
                                    .listRowBackground(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                    }))
                            } else {
                                Button(action: {
                                    withAnimation {
                                        proxy.scrollTo(self.topID, anchor: .bottom)
                                        self.path.append(word)
                                    }
                                }) {
                                    HStack(alignment: .center, spacing: 16.0) {
                                        Text(word.name)
                                            .foregroundColor(Color(UIColor {
                                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                            }))
                                            .font(.subheadline)
                                            .fontWeight(.semibold)
                                        Spacer()
                                        
                                        if word.attributes.isEmpty {
                                            Text("Modifier")
                                                .foregroundColor(Color(uiColor: self.accent))
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
                                            Text(String(format: "%ld", word.attributes.count))
                                                .foregroundColor(Color(uiColor: self.accent))
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
                                        
                                        Image(systemName: "chevron.right")
                                            .frame(
                                                alignment: .center
                                            )
                                            .background(.clear)
                                            .foregroundColor(Color(uiColor: self.accent))
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
                                .listRowBackground(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                }))
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
    
    private func makeAttributes(word: Word) -> some View {
        return Section(header: Text("Attributes")
            .foregroundColor(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
            }))
                .fontWeight(.semibold)
                .lineLimit(1)
                .textCase(.uppercase)) {
                    ForEach(self.attributes, id: \.self) { attribute in
                        if let index = self.words.firstIndex(where: { $0.id == word.id }) {
                            Button(action: {
                                if let i = self.words[index].attributes.firstIndex(where: { $0 == attribute }) {
                                    withAnimation {
                                        _ = self.words[index].attributes.remove(at: i)
                                    }
                                    
                                    self.save(words: self.words)
                                } else {
                                    withAnimation {
                                        self.words[index].attributes.append(attribute)
                                    }
                                    
                                    self.save(words: self.words)
                                }
                            }) {
                                HStack(alignment: .center, spacing: 16.0) {
                                    Text(attribute)
                                        .foregroundColor(Color(UIColor {
                                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                        }))
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
                                        .foregroundColor(Color(self.accent))
                                        .font(
                                            .system(size: 16.0)
                                        )
                                        .bold()
                                        .opacity(self.words[index].attributes.contains(attribute) ? 1.0 : 0.0)
                                        .animation(.linear(duration: 0.5), value: self.words[index].attributes.contains(attribute))
                                }
                                .frame(
                                    maxWidth: .infinity
                                )
                                .contentShape(Rectangle())
                            }
                            .buttonStyle(PlainButtonStyle())
                            .listRowBackground(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                            }))
                        }
                    }
                    .transition(.opacity.animation(.linear))
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
        withAnimation(.easeInOut(duration: 0.5)) {
            self.isRecording = true
        }
        
        self.speechRecognizer = SFSpeechRecognizer()
        
        Task {
            if await withCheckedContinuation({ continuation in
                SFSpeechRecognizer.requestAuthorization { status in
                    continuation.resume(returning: status == .authorized)
                }
            }), await AVAudioApplication.requestRecordPermission(), await (Task.detached {
                let audioSession = AVAudioSession.sharedInstance()
                
                if audioSession.category != .playAndRecord {
                    do {
                        try audioSession.setCategory(.playAndRecord, mode: .measurement, options: .duckOthers)
                        try audioSession.setActive(true, options: .notifyOthersOnDeactivation)
                    } catch {
                        return false
                    }
                }
                
                return true
            }.value), let (audioEngine, inputNode) = await {
                let audioEngine = AVAudioEngine()
                
                return (audioEngine, await Task.detached { audioEngine.inputNode }.value)
            }(), let recognizer = self.speechRecognizer, recognizer.isAvailable {
                let request = SFSpeechAudioBufferRecognitionRequest()
                
                request.shouldReportPartialResults = true
                
                inputNode.installTap(onBus: 0, bufferSize: 1024, format: inputNode.outputFormat(forBus: 0)) { (buffer: AVAudioPCMBuffer, when: AVAudioTime) in
                    request.append(buffer)
                    
                    if let floatChannelData = buffer.floatChannelData {
                        let pointee = floatChannelData.pointee
                        let rms = sqrt(stride(from: 0, to: Int(buffer.frameLength), by: buffer.stride).map { pointee[$0] }.map { $0 * $0 }.reduce(0, +) / Float(buffer.frameLength))
                        let dB = rms == 0.0 ? 0.0 : 20.0 * log10(rms)
                        let minimum: Float = -50.0
                        let maximum: Float = -25.0
                        let level = Double(dB > maximum ? 1.0 : (abs(minimum) - abs(max(dB, minimum))) / (abs(minimum) - abs(maximum)))
                        
                        Task {
                            await MainActor.run {
                                withAnimation(.easeInOut(duration: 0.5)) {
                                    self.volumeLevel = level
                                }
                            }
                        }
                    }
                }
                
                audioEngine.prepare()
                
                do {
                    try audioEngine.start()
                } catch {
                    self.speechRecognizer = nil
                    
                    withAnimation(.easeInOut(duration: 0.5)) {
                        self.isRecording = false
                    }
                    
                    return
                }
                
                self.audioEngine = audioEngine
                self.speechAudioBufferRecognitionRequest = request
                self.speechRecognitionTask = recognizer.recognitionTask(with: request, resultHandler: { result, error in
                    if error == nil {
                        if let result {
                            let text = result.bestTranscription.formattedString
                            
                            if result.isFinal && audioEngine.isRunning {
                                audioEngine.stop()
                                audioEngine.inputNode.removeTap(onBus: 0)
                                
                                withAnimation(.easeInOut(duration: 0.5)) {
                                    self.volumeLevel = 0.0
                                }
                            }
                            
                            if !text.isEmpty {
                                self.input = text
                            }
                        }
                    } else if audioEngine.isRunning {
                        audioEngine.stop()
                        audioEngine.inputNode.removeTap(onBus: 0)
                        
                        withAnimation(.easeInOut(duration: 0.5)) {
                            self.volumeLevel = 0.0
                        }
                    }
                })
            } else {
                self.speechRecognizer = nil
                
                withAnimation(.easeInOut(duration: 0.5)) {
                    self.isRecording = false
                }
            }
        }
    }
    
    private func stopRecognize() {
        if self.speechRecognizer != nil {
            if let speechRecognitionTask = self.speechRecognitionTask {
                speechRecognitionTask.cancel()
                self.speechRecognitionTask = nil
            }
            
            if let audioEngine = self.audioEngine {
                if audioEngine.isRunning {
                    audioEngine.stop()
                    audioEngine.inputNode.removeTap(onBus: 0)
                }
                
                self.audioEngine = nil
            }
            
            if let speechAudioBufferRecognitionRequest = self.speechAudioBufferRecognitionRequest {
                speechAudioBufferRecognitionRequest.endAudio()
                self.speechAudioBufferRecognitionRequest = nil
            }
            
            self.speechRecognizer = nil
            
            withAnimation(.easeInOut(duration: 0.5)) {
                self.isRecording = false
                self.volumeLevel = 0.0
            }
        }
    }
}

struct Camera: View {
    @Binding var text: String
    @Environment(\.dismiss) private var dismiss
    @State private var isRecognizable = true
    @State private var isPaused = false
    @State private var recognizeRegion = CGRect.zero
    @State private var recognizedText = String()
    
    var body: some View {
        NavigationStack {
            ZStack {
                Capture(isRecognizable: self.$isRecognizable, isPaused: self.isPaused, recognizeRegion: self.$recognizeRegion, recognizedText: self.$recognizedText)
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
                .stroke(Color(UIColor(white: 1.0, alpha: 1.0)), lineWidth: 2.0)
                
                if !self.isRecognizable {
                    Image(systemName: "exclamationmark.triangle")
                        .frame(
                            width: 16.0,
                            height: 16.0,
                            alignment: .center
                        )
                        .background(.clear)
                        .foregroundColor(Color(UIColor(white: 1.0, alpha: 1.0)))
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
                        .foregroundColor(Color(UIColor(white: 1.0, alpha: 1.0)))
                        .font(.system(size: 16.0))
                        .bold()
                } else {
                    Button(action: {
                        self.text = self.recognizedText
                        
                        dismiss()
                    }) {
                        ZStack {
                            Prompt(input: (self.recognizedText, nil, false, nil, CACurrentMediaTime()), accent: UIColor(white: 1.0, alpha: 1.0), font: UIFont.systemFont(ofSize: round(UIFontDescriptor.preferredFontDescriptor(withTextStyle: .callout).pointSize * 2.0), weight: .semibold))
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
            .background(Color(UIColor(white: 0.0, alpha: 1.0)))
            .navigationBarTitleDisplayMode(.inline)
            .toolbarBackground(.hidden, for: .navigationBar)
            .toolbar {
                ToolbarItem(placement: .navigationBarLeading) {
                    Button(action: {
                        dismiss()
                    }) {
                        Circle()
                            .fill(Color(UIColor(white: 1.0, alpha: 1.0)))
                            .frame(
                                width: 32.0,
                                height: 32.0
                            )
                            .overlay(Image(systemName: "xmark")
                                .frame(
                                    alignment: .center
                                )
                                    .background(.clear)
                                    .foregroundColor(Color(UIColor(white: 0.0, alpha: 1.0)))
                                    .font(.system(size: 8.0))
                                    .bold())
                    }
                    .background(.clear)
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
    
    init(text: Binding<String>) {
        self._text = text
    }
}

struct Capture: UIViewControllerRepresentable {
    @Binding private var isRecognizable: Bool
    private var isPaused: Bool
    @Binding private var recognizeRegion: CGRect
    @Binding private var recognizedText: String
    
    init(isRecognizable: Binding<Bool>, isPaused: Bool, recognizeRegion: Binding<CGRect>, recognizedText: Binding<String>) {
        self._isRecognizable = isRecognizable
        self.isPaused = isPaused
        self._recognizeRegion = recognizeRegion
        self._recognizedText = recognizedText
    }
    
    func makeUIViewController(context: Context) -> CaptureViewController {
        let viewController = CaptureViewController()
        
        viewController.delegate = context.coordinator
        
        return viewController
    }
    
    func updateUIViewController(_ uiViewController: CaptureViewController, context: Context) {
        if self.isPaused != uiViewController.isPaused {
            uiViewController.isPaused = self.isPaused
        }
    }
    
    func makeCoordinator() -> Coordinator {
        return Coordinator(self)
    }
    
    protocol CaptureDelegate: AnyObject {
        func captureDidUpdate(_ capture: CaptureViewController)
        func captureDidFail(_ capture: CaptureViewController)
    }
    
    class Coordinator: NSObject, CaptureDelegate {
        private var parent: Capture
        
        init(_ parent: Capture) {
            self.parent = parent
        }
        
        func captureDidUpdate(_ capture: Capture.CaptureViewController) {
            if !self.parent.recognizeRegion.equalTo(capture.recognizeRegion) {
                self.parent.recognizeRegion = capture.recognizeRegion
            }
            
            if self.parent.recognizedText != capture.recognizedText {
                self.parent.recognizedText = capture.recognizedText
            }
        }
        
        func captureDidFail(_ capture: Capture.CaptureViewController) {
            self.parent.isRecognizable = capture.isRecognizable
        }
    }
    
    class CaptureViewController: UIViewController, AVCaptureVideoDataOutputSampleBufferDelegate {
        var delegate: CaptureDelegate? = nil
        var isRecognizable = true
        var isPaused = false
        var recognizedText = String()
        var recognizeRegion = CGRect.zero
        private let sessionQueue = DispatchQueue(label: String(describing: Capture.CaptureViewController.self))
        private let captureSession = AVCaptureSession()
        private var captureVideoPreviewLayer: AVCaptureVideoPreviewLayer? = nil
        private var recognizeTextRequest: VNRecognizeTextRequest? = nil
        private var elapsedTime = 0.0
        
        override func viewDidLoad() {
            super.viewDidLoad()
            
            let recognizeTextRequest = VNRecognizeTextRequest { (request, error) in
                if let results = request.results as? [VNRecognizedTextObservation] {
                    var maxConfidence: VNConfidence = 0.0
                    var text: String? = nil
                    
                    for recognizedTextObservation in results {
                        if let first = recognizedTextObservation.topCandidates(1).first, first.confidence > maxConfidence {
                            text = first.string
                            maxConfidence = first.confidence
                        }
                    }
                    
                    if let text, maxConfidence >= 0.5 {
                        self.recognizedText = text.replacingOccurrences(of: "\n", with: "").trimmingCharacters(in: .whitespaces)
                        self.delegate?.captureDidUpdate(self)
                        
                        return
                    }
                }
                
                self.recognizedText = String()
                self.delegate?.captureDidUpdate(self)
            }
            
            recognizeTextRequest.preferBackgroundProcessing = true
            recognizeTextRequest.usesLanguageCorrection = true
            recognizeTextRequest.recognitionLevel = .accurate
            
            self.recognizeRegion = self.createRecognizeRegion(size: self.view.frame.size)
            
            if let preferredLanguage = Locale.preferredLanguages.first, let languageCode = Locale(identifier: preferredLanguage).language.languageCode, let languages = try? recognizeTextRequest.supportedRecognitionLanguages(), let language = languages.first(where: { Locale(identifier: $0).language.languageCode == languageCode }) {
                recognizeTextRequest.recognitionLanguages = [language]
                self.recognizeTextRequest = recognizeTextRequest
            } else {
                self.isRecognizable = false
                self.delegate?.captureDidFail(self)
                
                return
            }
            
            switch AVCaptureDevice.authorizationStatus(for: .video) {
            case .authorized:
                break
                
            case .notDetermined:
                self.sessionQueue.suspend()
                
                AVCaptureDevice.requestAccess(for: .video, completionHandler: { granted in
                    if !granted {
                        self.isRecognizable = false
                        self.delegate?.captureDidFail(self)
                    }
                    
                    self.sessionQueue.resume()
                })
                
            default:
                self.isRecognizable = false
                self.delegate?.captureDidFail(self)
            }
            
            if self.isRecognizable {
                self.captureVideoPreviewLayer = AVCaptureVideoPreviewLayer(session: self.captureSession)
                self.captureVideoPreviewLayer!.videoGravity = AVLayerVideoGravity.resizeAspectFill
                self.captureVideoPreviewLayer!.frame = CGRect(origin: CGPoint.zero, size: self.view.frame.size)
                
                self.view.layer.addSublayer(self.captureVideoPreviewLayer!)
                
                if let captureDevice = AVCaptureDevice.default(for: AVMediaType.video), let input = try? AVCaptureDeviceInput(device: captureDevice), self.captureSession.canAddInput(input) {
                    
                    let output = AVCaptureVideoDataOutput()
                    
                    self.captureSession.beginConfiguration()
                    self.captureSession.addInput(input)
                    
                    output.videoSettings = [kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32BGRA]
                    output.setSampleBufferDelegate(self, queue: self.sessionQueue)
                    output.alwaysDiscardsLateVideoFrames = true
                    
                    if self.captureSession.canAddOutput(output) {
                        self.captureSession.addOutput(output)
                    } else {
                        self.captureSession.commitConfiguration()
                        self.isRecognizable = false
                        self.delegate?.captureDidFail(self)
                        
                        return
                    }
                    
                    if self.captureSession.canSetSessionPreset(.photo) {
                        self.captureSession.sessionPreset = .photo
                    }
                    
                    self.captureSession.commitConfiguration()
                } else {
                    self.isRecognizable = false
                    self.delegate?.captureDidFail(self)
                }
            }
        }
        
        override func viewDidLayoutSubviews() {
            if let captureVideoPreviewLayer = self.captureVideoPreviewLayer {
                captureVideoPreviewLayer.frame = CGRect(origin: CGPoint.zero, size: self.view.frame.size)
            }
            
            self.recognizeRegion = self.createRecognizeRegion(size: self.view.frame.size)
            self.delegate?.captureDidUpdate(self)
        }
        
        override func viewWillTransition(to size: CGSize, with coordinator: UIViewControllerTransitionCoordinator) {
            if let captureVideoPreviewLayer = self.captureVideoPreviewLayer {
                captureVideoPreviewLayer.frame = CGRect(origin: CGPoint.zero, size: size)
                
                switch UIDevice.current.orientation {
                case UIDeviceOrientation.portraitUpsideDown:
                    captureVideoPreviewLayer.connection?.videoRotationAngle = 270
                case UIDeviceOrientation.landscapeLeft:
                    captureVideoPreviewLayer.connection?.videoRotationAngle = 0
                case UIDeviceOrientation.landscapeRight:
                    captureVideoPreviewLayer.connection?.videoRotationAngle = 180
                default:
                    captureVideoPreviewLayer.connection?.videoRotationAngle = 90
                }
            }
            
            self.recognizeRegion = self.createRecognizeRegion(size: size)
            self.delegate?.captureDidUpdate(self)
        }
        
        override func viewWillAppear(_ animated: Bool) {
            super.viewWillAppear(animated)
            
            if self.isRecognizable {
                self.sessionQueue.async {
                    self.captureSession.startRunning()
                    
                    Task {
                        await MainActor.run { [weak self] in
                            if let captureVideoPreviewLayer = self?.captureVideoPreviewLayer {
                                switch UIDevice.current.orientation {
                                case UIDeviceOrientation.portraitUpsideDown:
                                    captureVideoPreviewLayer.connection?.videoRotationAngle = 270
                                case UIDeviceOrientation.landscapeLeft:
                                    captureVideoPreviewLayer.connection?.videoRotationAngle = 0
                                case UIDeviceOrientation.landscapeRight:
                                    captureVideoPreviewLayer.connection?.videoRotationAngle = 180
                                default:
                                    captureVideoPreviewLayer.connection?.videoRotationAngle = 90
                                }
                            }
                        }
                    }
                }
            }
        }
        
        override func viewWillDisappear(_ animated: Bool) {
            if self.isRecognizable {
                self.sessionQueue.async {
                    self.captureSession.stopRunning()
                }
            }
            
            super.viewWillDisappear(animated)
        }
        
        func captureOutput(_ output: AVCaptureOutput, didOutput sampleBuffer: CMSampleBuffer, from connection: AVCaptureConnection) {
            if let pixelBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) {
                let image = CIImage(cvImageBuffer: pixelBuffer)
                
                Task {
                    let currentMediaTime = CACurrentMediaTime()
                    
                    if !self.isPaused && currentMediaTime - self.elapsedTime >= 1.0, let window = UIApplication.shared.connectedScenes.first as? UIWindowScene, let recognizeTextRequest = self.recognizeTextRequest {
                        let outputWidth = Double(CVPixelBufferGetWidth(pixelBuffer)) / window.screen.scale
                        let outputHeight = Double(CVPixelBufferGetHeight(pixelBuffer)) / window.screen.scale
                        let scale = max(self.view.frame.width / outputWidth, self.view.frame.height / outputHeight)
                        let width = outputWidth * scale
                        let height = outputHeight * scale
                        let offsetX = (width - self.view.frame.width) / 2.0
                        let offsetY = (height - self.view.frame.height) / 2.0
                        
                        recognizeTextRequest.regionOfInterest = CGRect(origin: CGPoint(x: (offsetX + self.recognizeRegion.origin.x) / width, y: (height - offsetY - self.recognizeRegion.origin.y - self.recognizeRegion.height) / height), size: CGSize(width: self.recognizeRegion.width / width, height: self.recognizeRegion.height / height))
                        
                        self.sessionQueue.async {
                            try? VNImageRequestHandler(ciImage: image, orientation: CGImagePropertyOrientation.up, options: [:]).perform([recognizeTextRequest])
                        }
                        self.elapsedTime = currentMediaTime
                    }
                }
            }
        }
        
        private func createRecognizeRegion(size: CGSize) -> CGRect {
            let length = min(size.width, size.height) * 0.75
            
            return CGRect(origin: CGPoint(x: (size.width - length) / 2.0, y: (size.height - length) / 2.0), size: CGSize(width: length, height: length))
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
    @Environment(\.dismiss) private var dismiss
    @Environment(\.openURL) var openURL
    @State private var paths: [String] = []
    @State private var characters: [(String?, String, Bool, CGImage?, Bool)] = []
    @State private var purchased: Set<String> = []
    @State private var isRestoring = false
    @State private var color: Color
    private let scaleRange: ClosedRange<Double>
    
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
                        .foregroundColor(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                        }))
                        .font(.headline)
                        .fontWeight(.semibold)
                        .lineLimit(1)
                        .textCase(.uppercase)
                }
                ToolbarItem(placement: .navigationBarLeading) {
                    Button(action: {
                        dismiss()
                    }) {
                        Circle()
                            .fill(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .frame(
                                width: 32.0,
                                height: 32.0
                            )
                            .overlay(Image(systemName: "xmark")
                                .frame(
                                    alignment: .center
                                )
                                .background(.clear)
                                .foregroundColor(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                }))
                                    .font(
                                        .system(size: 8.0)
                                    )
                                    .bold())
                    }
                    .background(.clear)
                }
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button(action: {
                        openURL(URL(string: "https://milchchan.com/")!)
                    }) {
                        Circle()
                            .fill(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .frame(
                                width: 32.0,
                                height: 32.0
                            )
                            .overlay(Image(systemName: "globe")
                                .frame(
                                    alignment: .center
                                )
                                .background(.clear)
                                .foregroundColor(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                                }))
                                    .font(
                                        .system(size: 8.0)
                                    )
                                    .bold())
                    }
                    .background(.clear)
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
        
        if let window = UIApplication.shared.connectedScenes.first as? UIWindowScene {
            self.scaleRange = 0.5...max(window.screen.scale, 2.0)
        } else {
            self.scaleRange = 0.5...2.0
        }
    }
    
    @ViewBuilder
    private func makeCharacters() -> some View {
        Section(header: Text("Characters")
            .foregroundColor(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
            }))
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
                                    .foregroundColor(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
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
                                        .foregroundColor(Color(self.accent))
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
                                            .foregroundColor(Color(uiColor: self.accent))
                                            .font(
                                                .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                                            )
                                        Text("Buy")
                                            .foregroundColor(Color(uiColor: self.accent))
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
                        .listRowBackground(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                        }))
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
                                .foregroundColor(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                }))
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
                    .listRowBackground(Color(UIColor {
                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                    }))
                    .transition(.opacity.animation(.linear))
                }
        Section {
            VStack(spacing: 8.0) {
                HStack(alignment: .center, spacing: 16.0) {
                    Text("Scale")
                        .foregroundColor(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                        }))
                        .font(.subheadline)
                        .fontWeight(.semibold)
                    Spacer()
                    Text(String(format: "%.1f", self.scale))
                        .foregroundColor(Color(uiColor: self.accent))
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
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .font(
                                .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                            )
                    } maximumValueLabel: {
                        Image(systemName: "plus.magnifyingglass")
                            .background(.clear)
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
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
            .listRowBackground(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
            }))
            .contentShape(Rectangle())
        }
        Section {
            Toggle("Sounds", isOn: Binding<Bool>(get: { !self.mute }, set: { self.mute = !$0 }))
                .foregroundColor(Color(UIColor {
                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                }))
                .font(.subheadline)
                .fontWeight(.semibold)
                .tint(Color(self.accent))
                .listRowBackground(Color(UIColor {
                    $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                }))
        }
    }
    
    private func makeAppearance() -> some View {
        return Section(header: Text("Appearance")
            .foregroundColor(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
            }))
                .fontWeight(.semibold)
                .lineLimit(1)
                .textCase(.uppercase)) {
                    ColorPicker("Accent", selection: self.$color, supportsOpacity: false)
                        .foregroundColor(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                        }))
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .listRowBackground(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                        }))
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
                            .foregroundColor(Color(UIColor {
                                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                            }))
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .frame(
                                maxWidth: .infinity,
                                alignment: .center
                            )
                            .contentShape(Rectangle())
                    }
                    .buttonStyle(PlainButtonStyle())
                    .listRowBackground(Color(UIColor {
                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                    }))
                }
    }
    
    private func makeIntelligence() -> some View {
        return Section(header: Text("Intelligence")
            .foregroundColor(Color(UIColor {
                $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
            }))
                .fontWeight(.semibold)
                .lineLimit(1)
                .textCase(.uppercase), footer: Button(action: {
                    openURL(URL(string: "https://milchchan.com/privacy")!)
                }) {
                    Text("Privacy")
                        .foregroundColor(Color(UIColor {
                            $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                        }))
                        .font(.footnote)
                        .fontWeight(.semibold)
                        .lineLimit(1)
                        .textCase(.uppercase)
                        .underline()
                        .padding(EdgeInsets(
                            top: 32.0,
                            leading: 0.0,
                            bottom: 0.0,
                            trailing: 0.0
                        ))
                        .frame(
                            maxWidth: .infinity,
                            alignment: .center
                        )
                }) {
                    VStack(spacing: 8.0) {
                        HStack(alignment: .center, spacing: 16.0) {
                            Text("Temperature")
                                .foregroundColor(Color(UIColor {
                                    $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                }))
                                .font(.subheadline)
                                .fontWeight(.semibold)
                            Spacer()
                            Text(String(format: "%.1f", self.temperature))
                                .foregroundColor(Color(uiColor: self.accent))
                                .font(.subheadline)
                                .fontWeight(.semibold)
                                .lineLimit(1)
                        }
                        HStack(alignment: .center, spacing: 0.0) {
                            Slider(value: self.$temperature, in: 0.0...2.0, step: Double.Stride(0.1)) {
                                EmptyView()
                            } minimumValueLabel: {
                                Image(systemName: "thermometer.low")
                                    .background(.clear)
                                    .foregroundColor(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
                                    .font(
                                        .system(size: UIFontDescriptor.preferredFontDescriptor(withTextStyle: .subheadline).pointSize)
                                    )
                            } maximumValueLabel: {
                                Image(systemName: "thermometer.high")
                                    .background(.clear)
                                    .foregroundColor(Color(UIColor {
                                        $0.userInterfaceStyle == .dark ? UIColor(white: 1.0, alpha: 1.0) : UIColor(white: 0.0, alpha: 1.0)
                                    }))
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
                    .listRowBackground(Color(UIColor {
                        $0.userInterfaceStyle == .dark ? UIColor(white: 0.0, alpha: 1.0) : UIColor(white: 1.0, alpha: 1.0)
                    }))
                    .contentShape(Rectangle())
                }
    }
    
    private func load() async -> ([String], [(String?, String, String, CGImage?)]) {
        return await Task.detached {
            var purchased: [String] = []
            var resolvedPaths: [(String?, String, String, CGImage?)] = []
            
            for await verificationResult in Transaction.currentEntitlements {
                guard case .verified(let transaction) = verificationResult else {
                    continue
                }
                
                if transaction.productType == .nonConsumable {
                    purchased.append(transaction.productID)
                }
            }
            
            if let window = await UIApplication.shared.connectedScenes.first as? UIWindowScene {
                let scale = Int(round(await window.screen.scale))
                let parser = Script.Parser()
                var languages: [String?] = []
                
                parser.excludeSequences = true
                
                if let preferredLanguage = Locale.preferredLanguages.first, let languageCode = Locale(identifier: preferredLanguage).language.languageCode {
                    languages.append(languageCode.identifier)
                }
                
                languages.append(nil)
                
                if FileManager.default.ubiquityIdentityToken != nil, let containerUrl = FileManager.default.url(forUbiquityContainerIdentifier: nil) {
                    let documentsUrl = containerUrl.appending(path: "Documents", directoryHint: .isDirectory)
                    let documentsPath = documentsUrl.path(percentEncoded: false)
                    var urlQueue: [(URL, String)] = [(documentsUrl, "Documents")]
                    var directories: [String] = []
                    
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
                            var paths: [String: [(URL, String, String?, String?, String, String?)]] = [:]
                            
                            for url in urls {
                                if let values = try? url.resourceValues(forKeys: [.nameKey]), let name = values.name, let match = name.wholeMatch(of: /^(.+?)(?:\.([a-z]{2,3}))?\.xml$/) {
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
                            
                            for language in languages {
                                var isResolved = false
                                
                                for value in paths.values {
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
                                }
                                
                                if isResolved {
                                    break
                                }
                            }
                        }
                    }
                }
                
                for resouce in ["Merku", "Milch"] {
                    var paths: [String: [(String, String, String?, String?, String, String?)]] = [:]
                    
                    for path in Bundle.main.paths(forResourcesOfType: "xml", inDirectory: resouce) {
                        let input = URL(filePath: path).deletingPathExtension().lastPathComponent
                        var characterId: String? = nil
                        var characterName: String? = nil
                        var characterPreview: String? = nil
                        
                        if let match = input.wholeMatch(of: /^(.+?)\.([a-z]{2,3})$/) {
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
                    
                    for language in languages {
                        var isResolved = false
                        
                        for value in paths.values {
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
                        }
                        
                        if isResolved {
                            break
                        }
                    }
                }
                
                resolvedPaths.sort { $0.2 < $1.2 }
            }
            
            return (purchased, resolvedPaths)
        }.value
    }
}

struct LearnIntent: AppIntent {
    static let title: LocalizedStringResource = "Learn"
    static var openAppWhenRun: Bool = true
    
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
    static var openAppWhenRun: Bool = true

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

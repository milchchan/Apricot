# -*- coding: utf-8 -*-
# Startup.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("mscorlib")
clr.AddReferenceByPartialName("System.Configuration")
clr.AddReferenceByPartialName("System.IO.Compression")
clr.AddReferenceByPartialName("System.Xml")
clr.AddReferenceByPartialName("System.Xml.Linq")
clr.AddReferenceByPartialName("WindowsBase")
clr.AddReferenceByPartialName("Apricot")

from System import Boolean, Byte, Int32, Double, String, StringComparison, Array, Type, Environment, Random
from System.IO import FileStream, MemoryStream, SeekOrigin, Path, Directory, File, FileMode, FileAccess, FileShare, SearchOption
from System.IO.Compression import ZipArchive
from System.Collections.Generic import List
from System.Configuration import ConfigurationManager, ConfigurationUserLevel, ExeConfigurationFileMap
from System.Globalization import CultureInfo
from System.Reflection import Assembly
from System.Xml.Linq import XDocument, XElement, XAttribute
from System.Xml.Serialization import XmlSerializer
from System.Windows import Point, Size
from Apricot import Character

config = None
directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly().GetName().Name)

if Directory.Exists(directory):
	fileName1 = Path.GetFileName(Assembly.GetEntryAssembly().Location)
		
	for fileName2 in Directory.EnumerateFiles(directory, "*.config"):
		if fileName1.Equals(Path.GetFileNameWithoutExtension(fileName2)):
			exeConfigurationFileMap = ExeConfigurationFileMap()
			exeConfigurationFileMap.ExeConfigFilename = fileName2
			config = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None)
	
if config is None:
	config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
	directory = None

if config.AppSettings.Settings["Characters"] is not None:
	path = config.AppSettings.Settings["Characters"].Value if directory is None else Path.Combine(directory, config.AppSettings.Settings["Characters"].Value)
	
	if not File.Exists(path):
		characterList = List[Character]()

		for fileName in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "*", SearchOption.AllDirectories):
			extension = Path.GetExtension(fileName)
		
			if extension.Equals(".xml", StringComparison.OrdinalIgnoreCase):
				fs = None

				try:
					fs = FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)
					rootElement = XDocument.Load(fs).Root

					if rootElement.Name.LocalName.Equals("script"):
						for characterElement in rootElement.Elements("character"):
							for sequenceElement in characterElement.Elements("sequence"):
								character = Character()
								originX = 0
								originY = 0
								x = 0
								y = 0
								width = 0
								height = 0

								for attribute in characterElement.Attributes():
									if attribute.Name.LocalName.Equals("name"):
										character.Name = attribute.Value
									elif attribute.Name.LocalName.Equals("origin-x"):
										originX = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("origin-y"):
										originY = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("x") or attribute.Name.LocalName.Equals("left"):
										x = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("y") or attribute.Name.LocalName.Equals("top"):
										y = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("width"):
										width = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
									elif attribute.Name.LocalName.Equals("height"):
										height = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)

								character.Origin = Point(originX, originY)
								character.BaseLocation = Point(x, y)
								character.Size = Size(width, height)
								character.Script = fileName

								characterList.Add(character)

								break

				except Exception, e:
					continue

				finally:
					if fs is not None:
						fs.Close()

			elif extension.Equals(".zip", StringComparison.OrdinalIgnoreCase):
				fs = None

				try:
					fs = FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)
				
					try:
						zipArchive = ZipArchive(fs)
						fs = None

						for zipArchiveEntry in zipArchive.Entries:
							if zipArchiveEntry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase):
								s = None

								try:
									s = zipArchiveEntry.Open()
									rootElement = XDocument.Load(s).Root

									if rootElement.Name.LocalName.Equals("script"):
										for characterElement in rootElement.Elements("character"):
											character = Character()
											originX = 0
											originY = 0
											x = 0
											y = 0
											width = 0
											height = 0

											for attribute in characterElement.Attributes():
												if attribute.Name.LocalName.Equals("name"):
													character.Name = attribute.Value
												elif attribute.Name.LocalName.Equals("origin-x"):
													originX = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
												elif attribute.Name.LocalName.Equals("origin-y"):
													originY = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
												elif attribute.Name.LocalName.Equals("x") or attribute.Name.LocalName.Equals("left"):
													x = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
												elif attribute.Name.LocalName.Equals("y") or attribute.Name.LocalName.Equals("top"):
													y = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
												elif attribute.Name.LocalName.Equals("width"):
													width = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)
												elif attribute.Name.LocalName.Equals("height"):
													height = Double.Parse(attribute.Value, CultureInfo.InvariantCulture)

											character.Origin = Point(originX, originY)
											character.BaseLocation = Point(x, y)
											character.Size = Size(width, height)
											character.Script = fileName

											characterList.Add(character)

								finally:
									if s is not None:
										s.Close()

					finally:
						zipArchive.Dispose()

				except Exception, e:
					continue

				finally:
					if fs is not None:
						fs.Close()
		
		ms = None
		fs = None

		try:
			ms = MemoryStream()
			characters = Array.CreateInstance(Character, 1)
			characters[0] = characterList[Random(Environment.TickCount).Next(characterList.Count)]
			serializer = XmlSerializer(characters.GetType())
			serializer.Serialize(ms, characters)
			ms.Seek(0, SeekOrigin.Begin)
			fs = FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)
			buffer = ms.ToArray()
			fs.Write(buffer, 0, buffer.Length)
			fs.Flush()

		finally:
			if fs is not None:
				fs.Close()
			
			if ms is not None:
				ms.Close()
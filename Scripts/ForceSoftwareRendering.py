# -*- coding: utf-8 -*-
# ForceSoftwareRendering.py
# Copyright © Masaaki Kawata All rights reserved.

import clr
clr.AddReferenceByPartialName("PresentationCore")

from System.Windows.Media import RenderOptions
from System.Windows.Interop import RenderMode

RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly
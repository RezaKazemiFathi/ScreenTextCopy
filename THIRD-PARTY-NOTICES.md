# Third-Party Notices

ScreenTextCopy itself is licensed under the **MIT License** — see
[LICENSE](LICENSE), Copyright (c) 2026 Reza Kazemi Fathi.

This file lists the third-party components that ScreenTextCopy uses or redistributes,
together with their licenses. **Redistributing the ScreenTextCopy installer or
portable build also redistributes these components, each under its own terms.** If
you repackage or ship the app, you are responsible for honouring the notices below.

The full text of the two most common licenses here — **Apache License 2.0** and the
**MIT License** — appears once each at the end of this document, and the table
references them instead of repeating them per component.

## Components

| Component | Version | License | Project |
|---|---|---|---|
| Tesseract OCR | 5 | Apache-2.0 ([full text](#apache-license-20)) | <https://github.com/tesseract-ocr/tesseract> |
| tessdata_fast trained models | — | Apache-2.0 ([full text](#apache-license-20)) | <https://github.com/tesseract-ocr/tessdata_fast> |
| CommunityToolkit.Mvvm | 8.2.2 | MIT ([full text](#mit-license)) | <https://github.com/CommunityToolkit/dotnet> |
| QRCoder | 1.6.0 | MIT ([full text](#mit-license)) | <https://github.com/codebude/QRCoder> |
| System.Drawing.Common | 8.0.18 | MIT ([full text](#mit-license)) | <https://github.com/dotnet/runtime> |
| Hardcodet.NotifyIcon.Wpf | 1.1.0 | CPOL 1.02 ([summary](#code-project-open-license-cpol-102)) | <https://github.com/hardcodet/wpf-notifyicon> |
| Vazirmatn font | — | SIL OFL 1.1 ([summary](#sil-open-font-license-11)) | <https://github.com/rastikerdar/vazirmatn> |
| .NET 8 runtime (redistributed in the self-contained build) | 8 | MIT ([full text](#mit-license)) | <https://github.com/dotnet/runtime> |
| MyMemory translation API (service, not bundled code) | — | Service terms ([note](#mymemory-translation-api)) | <https://mymemory.translated.net/> |

## Notes per component

### Tesseract OCR 5

The OCR engine, licensed under the Apache License 2.0. The release build ships the
Windows binaries from the **UB-Mannheim** build, which bundle a number of
dependencies alongside the engine itself, including **Leptonica, ICU, libpng,
libjpeg, libtiff, zlib, freetype, harfbuzz** and other MinGW runtime DLLs.

**Each of those bundled libraries retains its own permissive license** (BSD-style,
zlib, libpng, IJG, FreeType/FTL, MIT and similar) rather than being relicensed under
Apache-2.0. Their individual license texts and copyright notices are distributed with
the upstream Tesseract Windows build and are available from their respective project
pages; none of them impose copyleft obligations on ScreenTextCopy.

Note that the engine is **not** present in the Git repository — `libtesseract-5.dll`
is around 106 MB, above GitHub's 100 MB per-file limit, so it is fetched by
`scripts\fetch-tesseract.ps1` and included only in released builds.

### tessdata_fast trained models

The OCR language data (the `.traineddata` files), licensed under the Apache License
2.0. English, Persian and Arabic are bundled with the app; the other language packs
are downloaded on request over HTTPS from the official
[`tesseract-ocr/tessdata_fast`](https://github.com/tesseract-ocr/tessdata_fast)
repository.

### CommunityToolkit.Mvvm 8.2.2

The MVVM library behind the app's `[ObservableProperty]` and `[RelayCommand]` source
generators. MIT licensed, Copyright (c) .NET Foundation and Contributors.

### QRCoder 1.6.0

Generates the "send to phone" QR code, entirely offline. MIT licensed, Copyright (c)
Raffael Herrmann.

### System.Drawing.Common 8.0.18

Used for bitmap handling around screen capture and OCR input. MIT licensed, part of
the .NET platform — Copyright (c) .NET Foundation and Contributors.

### Hardcodet.NotifyIcon.Wpf 1.1.0

The WPF system-tray icon component, by Philipp Sumi, licensed under the
**Code Project Open License (CPOL) 1.02** — full text at
<https://www.codeproject.com/info/cpol10.aspx>.

Summary of the CPOL 1.02, provided for convenience only — the linked text governs:
the author grants a perpetual, worldwide, royalty-free license to reproduce, modify
and distribute the work and derivative works, including in commercial and closed
products, provided that the source code is **not** redistributed as a stand-alone
work whose primary purpose is the source itself, that you do not remove the author's
notices, and that you do not use the author's name or trademarks to endorse your
product. The work is provided **as-is, without warranties**, and the author is not
liable for any damages. ScreenTextCopy uses the component as a compiled NuGet
dependency, which is exactly the intended use.

### Vazirmatn font

The Persian/Arabic-script UI font by Saber Rastikerdar, licensed under the
**SIL Open Font License, Version 1.1** — full text at
<https://openfontlicense.org/> and in the
[font's repository](https://github.com/rastikerdar/vazirmatn).

Summary of the OFL 1.1, provided for convenience only — the linked text governs: the
font may be used, studied, modified and redistributed freely, including bundled with
and sold alongside software, as long as the font itself is not sold on its own, the
copyright and license notices travel with it, and any **modified** version is not
distributed under the Reserved Font Name. ScreenTextCopy bundles the font unmodified.

### .NET 8 runtime

The self-contained build embeds the .NET 8 runtime and WPF libraries so that no
separate .NET installation is required. MIT licensed, Copyright (c) .NET Foundation
and Contributors. Portions of the runtime are covered by additional third-party
notices published in the [dotnet/runtime](https://github.com/dotnet/runtime)
repository's own `THIRD-PARTY-NOTICES.TXT`.

### MyMemory translation API

The free translation provider calls the **MyMemory** web service at
<https://mymemory.translated.net/>. This is a **remote service, not bundled code** —
no MyMemory source or binary is redistributed with ScreenTextCopy. When you use the
free provider, the text you choose to translate is sent to that service, and
**MyMemory's own terms of service and privacy policy apply** to that request. Use the
custom OpenAI-compatible provider, or no translation at all, if you would rather not
involve it.

---

## MIT License

Applies to: CommunityToolkit.Mvvm, QRCoder, System.Drawing.Common, the .NET 8
runtime, and ScreenTextCopy itself. Copyright is held by each component's respective
copyright holders as named above.

```
MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## Apache License 2.0

Applies to: Tesseract OCR 5 and the tessdata_fast trained models.

```
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS
```

---

## Reporting a missing notice

If a component is missing from this file, or an attribution here is wrong, please
open an [issue](https://github.com/rezakazemifathi/ScreenTextCopy/issues) — license
attribution errors are treated as bugs and fixed in the next release.

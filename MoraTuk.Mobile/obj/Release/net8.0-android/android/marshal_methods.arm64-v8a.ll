; ModuleID = 'marshal_methods.arm64-v8a.ll'
source_filename = "marshal_methods.arm64-v8a.ll"
target datalayout = "e-m:e-i8:8:32-i16:16:32-i64:64-i128:128-n32:64-S128"
target triple = "aarch64-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [135 x ptr] zeroinitializer, align 8

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [270 x i64] [
	i64 98382396393917666, ; 0: Microsoft.Extensions.Primitives.dll => 0x15d8644ad360ce2 => 50
	i64 120698629574877762, ; 1: Mono.Android => 0x1accec39cafe242 => 134
	i64 131669012237370309, ; 2: Microsoft.Maui.Essentials.dll => 0x1d3c844de55c3c5 => 54
	i64 196720943101637631, ; 3: System.Linq.Expressions.dll => 0x2bae4a7cd73f3ff => 99
	i64 210515253464952879, ; 4: Xamarin.AndroidX.Collection.dll => 0x2ebe681f694702f => 61
	i64 232391251801502327, ; 5: Xamarin.AndroidX.SavedState.dll => 0x3399e9cbc897277 => 78
	i64 435118502366263740, ; 6: Xamarin.AndroidX.Security.SecurityCrypto.dll => 0x609d9f8f8bdb9bc => 79
	i64 545109961164950392, ; 7: fi/Microsoft.Maui.Controls.resources.dll => 0x7909e9f1ec38b78 => 7
	i64 750875890346172408, ; 8: System.Threading.Thread => 0xa6ba5a4da7d1ff8 => 127
	i64 799765834175365804, ; 9: System.ComponentModel.dll => 0xb1956c9f18442ac => 93
	i64 849051935479314978, ; 10: hi/Microsoft.Maui.Controls.resources.dll => 0xbc8703ca21a3a22 => 10
	i64 872800313462103108, ; 11: Xamarin.AndroidX.DrawerLayout => 0xc1ccf42c3c21c44 => 66
	i64 1120440138749646132, ; 12: Xamarin.Google.Android.Material.dll => 0xf8c9a5eae431534 => 83
	i64 1121665720830085036, ; 13: nb/Microsoft.Maui.Controls.resources.dll => 0xf90f507becf47ac => 18
	i64 1369545283391376210, ; 14: Xamarin.AndroidX.Navigation.Fragment.dll => 0x13019a2dd85acb52 => 74
	i64 1476839205573959279, ; 15: System.Net.Primitives.dll => 0x147ec96ece9b1e6f => 106
	i64 1486715745332614827, ; 16: Microsoft.Maui.Controls.dll => 0x14a1e017ea87d6ab => 51
	i64 1513467482682125403, ; 17: Mono.Android.Runtime => 0x1500eaa8245f6c5b => 133
	i64 1537168428375924959, ; 18: System.Threading.Thread.dll => 0x15551e8a954ae0df => 127
	i64 1556147632182429976, ; 19: ko/Microsoft.Maui.Controls.resources.dll => 0x15988c06d24c8918 => 16
	i64 1624659445732251991, ; 20: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0x168bf32877da9957 => 59
	i64 1628611045998245443, ; 21: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0x1699fd1e1a00b643 => 71
	i64 1743969030606105336, ; 22: System.Memory.dll => 0x1833d297e88f2af8 => 101
	i64 1767386781656293639, ; 23: System.Private.Uri.dll => 0x188704e9f5582107 => 115
	i64 1795316252682057001, ; 24: Xamarin.AndroidX.AppCompat.dll => 0x18ea3e9eac997529 => 58
	i64 1835311033149317475, ; 25: es\Microsoft.Maui.Controls.resources => 0x197855a927386163 => 6
	i64 1836611346387731153, ; 26: Xamarin.AndroidX.SavedState => 0x197cf449ebe482d1 => 78
	i64 1881198190668717030, ; 27: tr\Microsoft.Maui.Controls.resources => 0x1a1b5bc992ea9be6 => 28
	i64 1897575647115118287, ; 28: Xamarin.AndroidX.Security.SecurityCrypto => 0x1a558aff4cba86cf => 79
	i64 1920760634179481754, ; 29: Microsoft.Maui.Controls.Xaml => 0x1aa7e99ec2d2709a => 52
	i64 1959996714666907089, ; 30: tr/Microsoft.Maui.Controls.resources.dll => 0x1b334ea0a2a755d1 => 28
	i64 1981742497975770890, ; 31: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x1b80904d5c241f0a => 70
	i64 1983698669889758782, ; 32: cs/Microsoft.Maui.Controls.resources.dll => 0x1b87836e2031a63e => 2
	i64 2019660174692588140, ; 33: pl/Microsoft.Maui.Controls.resources.dll => 0x1c07463a6f8e1a6c => 20
	i64 2262844636196693701, ; 34: Xamarin.AndroidX.DrawerLayout.dll => 0x1f673d352266e6c5 => 66
	i64 2287834202362508563, ; 35: System.Collections.Concurrent => 0x1fc00515e8ce7513 => 87
	i64 2302323944321350744, ; 36: ru/Microsoft.Maui.Controls.resources.dll => 0x1ff37f6ddb267c58 => 24
	i64 2329709569556905518, ; 37: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x2054ca829b447e2e => 69
	i64 2335503487726329082, ; 38: System.Text.Encodings.Web => 0x2069600c4d9d1cfa => 123
	i64 2470498323731680442, ; 39: Xamarin.AndroidX.CoordinatorLayout => 0x2248f922dc398cba => 62
	i64 2497223385847772520, ; 40: System.Runtime => 0x22a7eb7046413568 => 120
	i64 2547086958574651984, ; 41: Xamarin.AndroidX.Activity.dll => 0x2359121801df4a50 => 57
	i64 2602673633151553063, ; 42: th\Microsoft.Maui.Controls.resources => 0x241e8de13a460e27 => 27
	i64 2632269733008246987, ; 43: System.Net.NameResolution => 0x2487b36034f808cb => 104
	i64 2656907746661064104, ; 44: Microsoft.Extensions.DependencyInjection => 0x24df3b84c8b75da8 => 44
	i64 2662981627730767622, ; 45: cs\Microsoft.Maui.Controls.resources => 0x24f4cfae6c48af06 => 2
	i64 2706075432581334785, ; 46: System.Net.WebSockets => 0x258de944be6c0701 => 112
	i64 2895129759130297543, ; 47: fi\Microsoft.Maui.Controls.resources => 0x282d912d479fa4c7 => 7
	i64 3017704767998173186, ; 48: Xamarin.Google.Android.Material => 0x29e10a7f7d88a002 => 83
	i64 3289520064315143713, ; 49: Xamarin.AndroidX.Lifecycle.Common => 0x2da6b911e3063621 => 68
	i64 3311221304742556517, ; 50: System.Numerics.Vectors.dll => 0x2df3d23ba9e2b365 => 113
	i64 3325875462027654285, ; 51: System.Runtime.Numerics => 0x2e27e21c8958b48d => 119
	i64 3328853167529574890, ; 52: System.Net.Sockets.dll => 0x2e327651a008c1ea => 109
	i64 3344514922410554693, ; 53: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x2e6a1a9a18463545 => 85
	i64 3429672777697402584, ; 54: Microsoft.Maui.Essentials => 0x2f98a5385a7b1ed8 => 54
	i64 3494946837667399002, ; 55: Microsoft.Extensions.Configuration => 0x30808ba1c00a455a => 42
	i64 3522470458906976663, ; 56: Xamarin.AndroidX.SwipeRefreshLayout => 0x30e2543832f52197 => 80
	i64 3551103847008531295, ; 57: System.Private.CoreLib.dll => 0x31480e226177735f => 131
	i64 3567343442040498961, ; 58: pt\Microsoft.Maui.Controls.resources => 0x3181bff5bea4ab11 => 22
	i64 3571415421602489686, ; 59: System.Runtime.dll => 0x319037675df7e556 => 120
	i64 3638003163729360188, ; 60: Microsoft.Extensions.Configuration.Abstractions => 0x327cc89a39d5f53c => 43
	i64 3647754201059316852, ; 61: System.Xml.ReaderWriter => 0x329f6d1e86145474 => 129
	i64 3655542548057982301, ; 62: Microsoft.Extensions.Configuration.dll => 0x32bb18945e52855d => 42
	i64 3727469159507183293, ; 63: Xamarin.AndroidX.RecyclerView => 0x33baa1739ba646bd => 77
	i64 3783726507060260521, ; 64: Microsoft.AspNetCore.SignalR.Common.dll => 0x34827f360c8e6ea9 => 40
	i64 3869221888984012293, ; 65: Microsoft.Extensions.Logging.dll => 0x35b23cceda0ed605 => 47
	i64 3890352374528606784, ; 66: Microsoft.Maui.Controls.Xaml.dll => 0x35fd4edf66e00240 => 52
	i64 3933965368022646939, ; 67: System.Net.Requests => 0x369840a8bfadc09b => 107
	i64 3966267475168208030, ; 68: System.Memory => 0x370b03412596249e => 101
	i64 4073500526318903918, ; 69: System.Private.Xml.dll => 0x3887fb25779ae26e => 116
	i64 4120493066591692148, ; 70: zh-Hant\Microsoft.Maui.Controls.resources => 0x392eee9cdda86574 => 33
	i64 4154383907710350974, ; 71: System.ComponentModel => 0x39a7562737acb67e => 93
	i64 4187479170553454871, ; 72: System.Linq.Expressions => 0x3a1cea1e912fa117 => 99
	i64 4205801962323029395, ; 73: System.ComponentModel.TypeConverter => 0x3a5e0299f7e7ad93 => 92
	i64 4356591372459378815, ; 74: vi/Microsoft.Maui.Controls.resources.dll => 0x3c75b8c562f9087f => 30
	i64 4522066533729196632, ; 75: MoraTuk.Mobile.dll => 0x3ec19b8db19c5a58 => 86
	i64 4679594760078841447, ; 76: ar/Microsoft.Maui.Controls.resources.dll => 0x40f142a407475667 => 0
	i64 4794310189461587505, ; 77: Xamarin.AndroidX.Activity => 0x4288cfb749e4c631 => 57
	i64 4795410492532947900, ; 78: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0x428cb86f8f9b7bbc => 80
	i64 4814660307502931973, ; 79: System.Net.NameResolution.dll => 0x42d11c0a5ee2a005 => 104
	i64 4853321196694829351, ; 80: System.Runtime.Loader.dll => 0x435a75ea15de7927 => 118
	i64 5103417709280584325, ; 81: System.Collections.Specialized => 0x46d2fb5e161b6285 => 89
	i64 5182934613077526976, ; 82: System.Collections.Specialized.dll => 0x47ed7b91fa9009c0 => 89
	i64 5290786973231294105, ; 83: System.Runtime.Loader => 0x496ca6b869b72699 => 118
	i64 5471532531798518949, ; 84: sv\Microsoft.Maui.Controls.resources => 0x4beec9d926d82ca5 => 26
	i64 5522859530602327440, ; 85: uk\Microsoft.Maui.Controls.resources => 0x4ca5237b51eead90 => 29
	i64 5570799893513421663, ; 86: System.IO.Compression.Brotli => 0x4d4f74fcdfa6c35f => 97
	i64 5573260873512690141, ; 87: System.Security.Cryptography.dll => 0x4d58333c6e4ea1dd => 121
	i64 5692067934154308417, ; 88: Xamarin.AndroidX.ViewPager2.dll => 0x4efe49a0d4a8bb41 => 82
	i64 5979151488806146654, ; 89: System.Formats.Asn1 => 0x52fa3699a489d25e => 96
	i64 6014447449592687183, ; 90: Microsoft.AspNetCore.Http.Connections.Common.dll => 0x53779c16e939ea4f => 37
	i64 6034224070161570862, ; 91: Microsoft.AspNetCore.SignalR.Client.dll => 0x53bdded235179c2e => 38
	i64 6068057819846744445, ; 92: ro/Microsoft.Maui.Controls.resources.dll => 0x5436126fec7f197d => 23
	i64 6200764641006662125, ; 93: ro\Microsoft.Maui.Controls.resources => 0x560d8a96830131ed => 23
	i64 6222399776351216807, ; 94: System.Text.Json.dll => 0x565a67a0ffe264a7 => 124
	i64 6357457916754632952, ; 95: _Microsoft.Android.Resource.Designer => 0x583a3a4ac2a7a0f8 => 34
	i64 6401687960814735282, ; 96: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0x58d75d486341cfb2 => 69
	i64 6478287442656530074, ; 97: hr\Microsoft.Maui.Controls.resources => 0x59e7801b0c6a8e9a => 11
	i64 6548213210057960872, ; 98: Xamarin.AndroidX.CustomView.dll => 0x5adfed387b066da8 => 65
	i64 6560151584539558821, ; 99: Microsoft.Extensions.Options => 0x5b0a571be53243a5 => 49
	i64 6743165466166707109, ; 100: nl\Microsoft.Maui.Controls.resources => 0x5d948943c08c43a5 => 19
	i64 6777482997383978746, ; 101: pt/Microsoft.Maui.Controls.resources.dll => 0x5e0e74e0a2525efa => 22
	i64 6783125919820072783, ; 102: Microsoft.AspNetCore.Connections.Abstractions => 0x5e228115e59ec74f => 35
	i64 6894844156784520562, ; 103: System.Numerics.Vectors => 0x5faf683aead1ad72 => 113
	i64 7017588408768804231, ; 104: Microsoft.AspNetCore.SignalR.Protocols.Json => 0x61637b7a1c903587 => 41
	i64 7220009545223068405, ; 105: sv/Microsoft.Maui.Controls.resources.dll => 0x6432a06d99f35af5 => 26
	i64 7270811800166795866, ; 106: System.Linq => 0x64e71ccf51a90a5a => 100
	i64 7274487178042942800, ; 107: MoraTuk.Mobile => 0x64f42b8bea622550 => 86
	i64 7377312882064240630, ; 108: System.ComponentModel.TypeConverter.dll => 0x66617afac45a2ff6 => 92
	i64 7489048572193775167, ; 109: System.ObjectModel => 0x67ee71ff6b419e3f => 114
	i64 7654504624184590948, ; 110: System.Net.Http => 0x6a3a4366801b8264 => 103
	i64 7708790323521193081, ; 111: ms/Microsoft.Maui.Controls.resources.dll => 0x6afb1ff4d1730479 => 17
	i64 7714652370974252055, ; 112: System.Private.CoreLib => 0x6b0ff375198b9c17 => 131
	i64 7735352534559001595, ; 113: Xamarin.Kotlin.StdLib.dll => 0x6b597e2582ce8bfb => 84
	i64 7836164640616011524, ; 114: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x6cbfa6390d64d704 => 59
	i64 8064050204834738623, ; 115: System.Collections.dll => 0x6fe942efa61731bf => 90
	i64 8083354569033831015, ; 116: Xamarin.AndroidX.Lifecycle.Common.dll => 0x702dd82730cad267 => 68
	i64 8085230611270010360, ; 117: System.Net.Http.Json.dll => 0x703482674fdd05f8 => 102
	i64 8087206902342787202, ; 118: System.Diagnostics.DiagnosticSource => 0x703b87d46f3aa082 => 95
	i64 8167236081217502503, ; 119: Java.Interop.dll => 0x7157d9f1a9b8fd27 => 132
	i64 8185542183669246576, ; 120: System.Collections => 0x7198e33f4794aa70 => 90
	i64 8243855692487634729, ; 121: Microsoft.AspNetCore.SignalR.Protocols.Json.dll => 0x72680f13124eaf29 => 41
	i64 8246048515196606205, ; 122: Microsoft.Maui.Graphics.dll => 0x726fd96f64ee56fd => 55
	i64 8368701292315763008, ; 123: System.Security.Cryptography => 0x7423997c6fd56140 => 121
	i64 8400357532724379117, ; 124: Xamarin.AndroidX.Navigation.UI.dll => 0x749410ab44503ded => 76
	i64 8563666267364444763, ; 125: System.Private.Uri => 0x76d841191140ca5b => 115
	i64 8614108721271900878, ; 126: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x778b763e14018ace => 21
	i64 8626175481042262068, ; 127: Java.Interop => 0x77b654e585b55834 => 132
	i64 8639588376636138208, ; 128: Xamarin.AndroidX.Navigation.Runtime => 0x77e5fbdaa2fda2e0 => 75
	i64 8677882282824630478, ; 129: pt-BR\Microsoft.Maui.Controls.resources => 0x786e07f5766b00ce => 21
	i64 8725526185868997716, ; 130: System.Diagnostics.DiagnosticSource.dll => 0x79174bd613173454 => 95
	i64 9045785047181495996, ; 131: zh-HK\Microsoft.Maui.Controls.resources => 0x7d891592e3cb0ebc => 31
	i64 9312692141327339315, ; 132: Xamarin.AndroidX.ViewPager2 => 0x813d54296a634f33 => 82
	i64 9324707631942237306, ; 133: Xamarin.AndroidX.AppCompat => 0x8168042fd44a7c7a => 58
	i64 9659729154652888475, ; 134: System.Text.RegularExpressions => 0x860e407c9991dd9b => 125
	i64 9678050649315576968, ; 135: Xamarin.AndroidX.CoordinatorLayout.dll => 0x864f57c9feb18c88 => 62
	i64 9702891218465930390, ; 136: System.Collections.NonGeneric.dll => 0x86a79827b2eb3c96 => 88
	i64 9808709177481450983, ; 137: Mono.Android.dll => 0x881f890734e555e7 => 134
	i64 9956195530459977388, ; 138: Microsoft.Maui => 0x8a2b8315b36616ac => 53
	i64 9991543690424095600, ; 139: es/Microsoft.Maui.Controls.resources.dll => 0x8aa9180c89861370 => 6
	i64 10038780035334861115, ; 140: System.Net.Http.dll => 0x8b50e941206af13b => 103
	i64 10051358222726253779, ; 141: System.Private.Xml => 0x8b7d990c97ccccd3 => 116
	i64 10078727084704864206, ; 142: System.Net.WebSockets.Client => 0x8bded4e257f117ce => 111
	i64 10092835686693276772, ; 143: Microsoft.Maui.Controls => 0x8c10f49539bd0c64 => 51
	i64 10143853363526200146, ; 144: da\Microsoft.Maui.Controls.resources => 0x8cc634e3c2a16b52 => 3
	i64 10226498071391929720, ; 145: Microsoft.Extensions.Features => 0x8debd1d049888578 => 46
	i64 10229024438826829339, ; 146: Xamarin.AndroidX.CustomView => 0x8df4cb880b10061b => 65
	i64 10406448008575299332, ; 147: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x906b2153fcb3af04 => 85
	i64 10430153318873392755, ; 148: Xamarin.AndroidX.Core => 0x90bf592ea44f6673 => 63
	i64 10506226065143327199, ; 149: ca\Microsoft.Maui.Controls.resources => 0x91cd9cf11ed169df => 1
	i64 10785150219063592792, ; 150: System.Net.Primitives => 0x95ac8cfb68830758 => 106
	i64 11002576679268595294, ; 151: Microsoft.Extensions.Logging.Abstractions => 0x98b1013215cd365e => 48
	i64 11009005086950030778, ; 152: Microsoft.Maui.dll => 0x98c7d7cc621ffdba => 53
	i64 11103970607964515343, ; 153: hu\Microsoft.Maui.Controls.resources => 0x9a193a6fc41a6c0f => 12
	i64 11162124722117608902, ; 154: Xamarin.AndroidX.ViewPager => 0x9ae7d54b986d05c6 => 81
	i64 11220793807500858938, ; 155: ja\Microsoft.Maui.Controls.resources => 0x9bb8448481fdd63a => 15
	i64 11226290749488709958, ; 156: Microsoft.Extensions.Options.dll => 0x9bcbcbf50c874146 => 49
	i64 11340910727871153756, ; 157: Xamarin.AndroidX.CursorAdapter => 0x9d630238642d465c => 64
	i64 11485890710487134646, ; 158: System.Runtime.InteropServices => 0x9f6614bf0f8b71b6 => 117
	i64 11513602507638267977, ; 159: System.IO.Pipelines.dll => 0x9fc8887aa0d36049 => 56
	i64 11518296021396496455, ; 160: id\Microsoft.Maui.Controls.resources => 0x9fd9353475222047 => 13
	i64 11529969570048099689, ; 161: Xamarin.AndroidX.ViewPager.dll => 0xa002ae3c4dc7c569 => 81
	i64 11530571088791430846, ; 162: Microsoft.Extensions.Logging => 0xa004d1504ccd66be => 47
	i64 11705530742807338875, ; 163: he/Microsoft.Maui.Controls.resources.dll => 0xa272663128721f7b => 9
	i64 12145679461940342714, ; 164: System.Text.Json => 0xa88e1f1ebcb62fba => 124
	i64 12313367145828839434, ; 165: System.IO.Pipelines => 0xaae1de2e1c17f00a => 56
	i64 12451044538927396471, ; 166: Xamarin.AndroidX.Fragment.dll => 0xaccaff0a2955b677 => 67
	i64 12466513435562512481, ; 167: Xamarin.AndroidX.Loader.dll => 0xad01f3eb52569061 => 72
	i64 12475113361194491050, ; 168: _Microsoft.Android.Resource.Designer.dll => 0xad2081818aba1caa => 34
	i64 12538491095302438457, ; 169: Xamarin.AndroidX.CardView.dll => 0xae01ab382ae67e39 => 60
	i64 12550732019250633519, ; 170: System.IO.Compression => 0xae2d28465e8e1b2f => 98
	i64 12681088699309157496, ; 171: it/Microsoft.Maui.Controls.resources.dll => 0xaffc46fc178aec78 => 14
	i64 12700543734426720211, ; 172: Xamarin.AndroidX.Collection => 0xb041653c70d157d3 => 61
	i64 12708922737231849740, ; 173: System.Text.Encoding.Extensions => 0xb05f29e50e96e90c => 122
	i64 12823819093633476069, ; 174: th/Microsoft.Maui.Controls.resources.dll => 0xb1f75b85abe525e5 => 27
	i64 12843321153144804894, ; 175: Microsoft.Extensions.Primitives => 0xb23ca48abd74d61e => 50
	i64 12859557719246324186, ; 176: System.Net.WebHeaderCollection.dll => 0xb276539ce04f41da => 110
	i64 13221551921002590604, ; 177: ca/Microsoft.Maui.Controls.resources.dll => 0xb77c636bdebe318c => 1
	i64 13222659110913276082, ; 178: ja/Microsoft.Maui.Controls.resources.dll => 0xb78052679c1178b2 => 15
	i64 13295219713260136977, ; 179: Microsoft.AspNetCore.Http.Connections.Client => 0xb8821be35ba42a11 => 36
	i64 13343850469010654401, ; 180: Mono.Android.Runtime.dll => 0xb92ee14d854f44c1 => 133
	i64 13381594904270902445, ; 181: he\Microsoft.Maui.Controls.resources => 0xb9b4f9aaad3e94ad => 9
	i64 13428779960367410341, ; 182: Microsoft.AspNetCore.SignalR.Client.Core.dll => 0xba5c9c39a8956ca5 => 39
	i64 13465488254036897740, ; 183: Xamarin.Kotlin.StdLib => 0xbadf06394d106fcc => 84
	i64 13467053111158216594, ; 184: uk/Microsoft.Maui.Controls.resources.dll => 0xbae49573fde79792 => 29
	i64 13540124433173649601, ; 185: vi\Microsoft.Maui.Controls.resources => 0xbbe82f6eede718c1 => 30
	i64 13545416393490209236, ; 186: id/Microsoft.Maui.Controls.resources.dll => 0xbbfafc7174bc99d4 => 13
	i64 13572454107664307259, ; 187: Xamarin.AndroidX.RecyclerView.dll => 0xbc5b0b19d99f543b => 77
	i64 13717397318615465333, ; 188: System.ComponentModel.Primitives.dll => 0xbe5dfc2ef2f87d75 => 91
	i64 13755568601956062840, ; 189: fr/Microsoft.Maui.Controls.resources.dll => 0xbee598c36b1b9678 => 8
	i64 13814445057219246765, ; 190: hr/Microsoft.Maui.Controls.resources.dll => 0xbfb6c49664b43aad => 11
	i64 13881769479078963060, ; 191: System.Console.dll => 0xc0a5f3cade5c6774 => 94
	i64 13959074834287824816, ; 192: Xamarin.AndroidX.Fragment => 0xc1b8989a7ad20fb0 => 67
	i64 14100563506285742564, ; 193: da/Microsoft.Maui.Controls.resources.dll => 0xc3af43cd0cff89e4 => 3
	i64 14124974489674258913, ; 194: Xamarin.AndroidX.CardView => 0xc405fd76067d19e1 => 60
	i64 14125464355221830302, ; 195: System.Threading.dll => 0xc407bafdbc707a9e => 128
	i64 14254574811015963973, ; 196: System.Text.Encoding.Extensions.dll => 0xc5d26c4442d66545 => 122
	i64 14461014870687870182, ; 197: System.Net.Requests.dll => 0xc8afd8683afdece6 => 107
	i64 14464374589798375073, ; 198: ru\Microsoft.Maui.Controls.resources => 0xc8bbc80dcb1e5ea1 => 24
	i64 14522721392235705434, ; 199: el/Microsoft.Maui.Controls.resources.dll => 0xc98b12295c2cf45a => 5
	i64 14551742072151931844, ; 200: System.Text.Encodings.Web.dll => 0xc9f22c50f1b8fbc4 => 123
	i64 14604329626201521481, ; 201: Microsoft.AspNetCore.SignalR.Client => 0xcaad006b00747d49 => 38
	i64 14669215534098758659, ; 202: Microsoft.Extensions.DependencyInjection.dll => 0xcb9385ceb3993c03 => 44
	i64 14705122255218365489, ; 203: ko\Microsoft.Maui.Controls.resources => 0xcc1316c7b0fb5431 => 16
	i64 14744092281598614090, ; 204: zh-Hans\Microsoft.Maui.Controls.resources => 0xcc9d89d004439a4a => 32
	i64 14809184851036126845, ; 205: Microsoft.AspNetCore.SignalR.Client.Core => 0xcd84cb28db1abe7d => 39
	i64 14852515768018889994, ; 206: Xamarin.AndroidX.CursorAdapter.dll => 0xce1ebc6625a76d0a => 64
	i64 14892012299694389861, ; 207: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xceab0e490a083a65 => 33
	i64 14904040806490515477, ; 208: ar\Microsoft.Maui.Controls.resources => 0xced5ca2604cb2815 => 0
	i64 14954917835170835695, ; 209: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xcf8a8a895a82ecef => 45
	i64 14984936317414011727, ; 210: System.Net.WebHeaderCollection => 0xcff5302fe54ff34f => 110
	i64 14987728460634540364, ; 211: System.IO.Compression.dll => 0xcfff1ba06622494c => 98
	i64 15015154896917945444, ; 212: System.Net.Security.dll => 0xd0608bd33642dc64 => 108
	i64 15024878362326791334, ; 213: System.Net.Http.Json => 0xd0831743ebf0f4a6 => 102
	i64 15076659072870671916, ; 214: System.ObjectModel.dll => 0xd13b0d8c1620662c => 114
	i64 15111608613780139878, ; 215: ms\Microsoft.Maui.Controls.resources => 0xd1b737f831192f66 => 17
	i64 15115185479366240210, ; 216: System.IO.Compression.Brotli.dll => 0xd1c3ed1c1bc467d2 => 97
	i64 15133485256822086103, ; 217: System.Linq.dll => 0xd204f0a9127dd9d7 => 100
	i64 15227001540531775957, ; 218: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd3512d3999b8e9d5 => 43
	i64 15370334346939861994, ; 219: Xamarin.AndroidX.Core.dll => 0xd54e65a72c560bea => 63
	i64 15391712275433856905, ; 220: Microsoft.Extensions.DependencyInjection.Abstractions => 0xd59a58c406411f89 => 45
	i64 15527772828719725935, ; 221: System.Console => 0xd77dbb1e38cd3d6f => 94
	i64 15536481058354060254, ; 222: de\Microsoft.Maui.Controls.resources => 0xd79cab34eec75bde => 4
	i64 15557562860424774966, ; 223: System.Net.Sockets => 0xd7e790fe7a6dc536 => 109
	i64 15582737692548360875, ; 224: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xd841015ed86f6aab => 71
	i64 15609085926864131306, ; 225: System.dll => 0xd89e9cf3334914ea => 130
	i64 15661133872274321916, ; 226: System.Xml.ReaderWriter.dll => 0xd9578647d4bfb1fc => 129
	i64 15664356999916475676, ; 227: de/Microsoft.Maui.Controls.resources.dll => 0xd962f9b2b6ecd51c => 4
	i64 15743187114543869802, ; 228: hu/Microsoft.Maui.Controls.resources.dll => 0xda7b09450ae4ef6a => 12
	i64 15783653065526199428, ; 229: el\Microsoft.Maui.Controls.resources => 0xdb0accd674b1c484 => 5
	i64 15847085070278954535, ; 230: System.Threading.Channels.dll => 0xdbec27e8f35f8e27 => 126
	i64 16018552496348375205, ; 231: System.Net.NetworkInformation.dll => 0xde4d54a020caa8a5 => 105
	i64 16154507427712707110, ; 232: System => 0xe03056ea4e39aa26 => 130
	i64 16156430004425724367, ; 233: Microsoft.AspNetCore.Http.Connections.Client.dll => 0xe0372b7d144211cf => 36
	i64 16219561732052121626, ; 234: System.Net.Security => 0xe1177575db7c781a => 108
	i64 16288847719894691167, ; 235: nb\Microsoft.Maui.Controls.resources => 0xe20d9cb300c12d5f => 18
	i64 16321164108206115771, ; 236: Microsoft.Extensions.Logging.Abstractions.dll => 0xe2806c487e7b0bbb => 48
	i64 16343918515847859304, ; 237: Microsoft.Extensions.Features.dll => 0xe2d1434bdf0a8c68 => 46
	i64 16454459195343277943, ; 238: System.Net.NetworkInformation => 0xe459fb756d988f77 => 105
	i64 16605226748660468415, ; 239: Microsoft.AspNetCore.SignalR.Common => 0xe6719dbfe8b8cabf => 40
	i64 16649148416072044166, ; 240: Microsoft.Maui.Graphics => 0xe70da84600bb4e86 => 55
	i64 16677317093839702854, ; 241: Xamarin.AndroidX.Navigation.UI => 0xe771bb8960dd8b46 => 76
	i64 16890310621557459193, ; 242: System.Text.RegularExpressions.dll => 0xea66700587f088f9 => 125
	i64 16942731696432749159, ; 243: sk\Microsoft.Maui.Controls.resources => 0xeb20acb622a01a67 => 25
	i64 16998075588627545693, ; 244: Xamarin.AndroidX.Navigation.Fragment => 0xebe54bb02d623e5d => 74
	i64 17008137082415910100, ; 245: System.Collections.NonGeneric => 0xec090a90408c8cd4 => 88
	i64 17031351772568316411, ; 246: Xamarin.AndroidX.Navigation.Common.dll => 0xec5b843380a769fb => 73
	i64 17062143951396181894, ; 247: System.ComponentModel.Primitives => 0xecc8e986518c9786 => 91
	i64 17089008752050867324, ; 248: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xed285aeb25888c7c => 32
	i64 17118171214553292978, ; 249: System.Threading.Channels => 0xed8ff6060fc420b2 => 126
	i64 17338386382517543202, ; 250: System.Net.WebSockets.Client.dll => 0xf09e528d5c6da122 => 111
	i64 17342750010158924305, ; 251: hi\Microsoft.Maui.Controls.resources => 0xf0add33f97ecc211 => 10
	i64 17438153253682247751, ; 252: sk/Microsoft.Maui.Controls.resources.dll => 0xf200c3fe308d7847 => 25
	i64 17509662556995089465, ; 253: System.Net.WebSockets.dll => 0xf2fed1534ea67439 => 112
	i64 17514990004910432069, ; 254: fr\Microsoft.Maui.Controls.resources => 0xf311be9c6f341f45 => 8
	i64 17571845317586269034, ; 255: Microsoft.AspNetCore.Connections.Abstractions.dll => 0xf3dbbc377ad7336a => 35
	i64 17623389608345532001, ; 256: pl\Microsoft.Maui.Controls.resources => 0xf492db79dfbef661 => 20
	i64 17636563193350668017, ; 257: Microsoft.AspNetCore.Http.Connections.Common => 0xf4c1a8c826653ef1 => 37
	i64 17702523067201099846, ; 258: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xf5abfef008ae1846 => 31
	i64 17704177640604968747, ; 259: Xamarin.AndroidX.Loader => 0xf5b1dfc36cac272b => 72
	i64 17710060891934109755, ; 260: Xamarin.AndroidX.Lifecycle.ViewModel => 0xf5c6c68c9e45303b => 70
	i64 17712670374920797664, ; 261: System.Runtime.InteropServices.dll => 0xf5d00bdc38bd3de0 => 117
	i64 17777860260071588075, ; 262: System.Runtime.Numerics.dll => 0xf6b7a5b72419c0eb => 119
	i64 18025913125965088385, ; 263: System.Threading => 0xfa28e87b91334681 => 128
	i64 18099568558057551825, ; 264: nl/Microsoft.Maui.Controls.resources.dll => 0xfb2e95b53ad977d1 => 19
	i64 18121036031235206392, ; 265: Xamarin.AndroidX.Navigation.Common => 0xfb7ada42d3d42cf8 => 73
	i64 18146411883821974900, ; 266: System.Formats.Asn1.dll => 0xfbd50176eb22c574 => 96
	i64 18245806341561545090, ; 267: System.Collections.Concurrent.dll => 0xfd3620327d587182 => 87
	i64 18305135509493619199, ; 268: Xamarin.AndroidX.Navigation.Runtime.dll => 0xfe08e7c2d8c199ff => 75
	i64 18324163916253801303 ; 269: it\Microsoft.Maui.Controls.resources => 0xfe4c81ff0a56ab57 => 14
], align 8

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [270 x i32] [
	i32 50, ; 0
	i32 134, ; 1
	i32 54, ; 2
	i32 99, ; 3
	i32 61, ; 4
	i32 78, ; 5
	i32 79, ; 6
	i32 7, ; 7
	i32 127, ; 8
	i32 93, ; 9
	i32 10, ; 10
	i32 66, ; 11
	i32 83, ; 12
	i32 18, ; 13
	i32 74, ; 14
	i32 106, ; 15
	i32 51, ; 16
	i32 133, ; 17
	i32 127, ; 18
	i32 16, ; 19
	i32 59, ; 20
	i32 71, ; 21
	i32 101, ; 22
	i32 115, ; 23
	i32 58, ; 24
	i32 6, ; 25
	i32 78, ; 26
	i32 28, ; 27
	i32 79, ; 28
	i32 52, ; 29
	i32 28, ; 30
	i32 70, ; 31
	i32 2, ; 32
	i32 20, ; 33
	i32 66, ; 34
	i32 87, ; 35
	i32 24, ; 36
	i32 69, ; 37
	i32 123, ; 38
	i32 62, ; 39
	i32 120, ; 40
	i32 57, ; 41
	i32 27, ; 42
	i32 104, ; 43
	i32 44, ; 44
	i32 2, ; 45
	i32 112, ; 46
	i32 7, ; 47
	i32 83, ; 48
	i32 68, ; 49
	i32 113, ; 50
	i32 119, ; 51
	i32 109, ; 52
	i32 85, ; 53
	i32 54, ; 54
	i32 42, ; 55
	i32 80, ; 56
	i32 131, ; 57
	i32 22, ; 58
	i32 120, ; 59
	i32 43, ; 60
	i32 129, ; 61
	i32 42, ; 62
	i32 77, ; 63
	i32 40, ; 64
	i32 47, ; 65
	i32 52, ; 66
	i32 107, ; 67
	i32 101, ; 68
	i32 116, ; 69
	i32 33, ; 70
	i32 93, ; 71
	i32 99, ; 72
	i32 92, ; 73
	i32 30, ; 74
	i32 86, ; 75
	i32 0, ; 76
	i32 57, ; 77
	i32 80, ; 78
	i32 104, ; 79
	i32 118, ; 80
	i32 89, ; 81
	i32 89, ; 82
	i32 118, ; 83
	i32 26, ; 84
	i32 29, ; 85
	i32 97, ; 86
	i32 121, ; 87
	i32 82, ; 88
	i32 96, ; 89
	i32 37, ; 90
	i32 38, ; 91
	i32 23, ; 92
	i32 23, ; 93
	i32 124, ; 94
	i32 34, ; 95
	i32 69, ; 96
	i32 11, ; 97
	i32 65, ; 98
	i32 49, ; 99
	i32 19, ; 100
	i32 22, ; 101
	i32 35, ; 102
	i32 113, ; 103
	i32 41, ; 104
	i32 26, ; 105
	i32 100, ; 106
	i32 86, ; 107
	i32 92, ; 108
	i32 114, ; 109
	i32 103, ; 110
	i32 17, ; 111
	i32 131, ; 112
	i32 84, ; 113
	i32 59, ; 114
	i32 90, ; 115
	i32 68, ; 116
	i32 102, ; 117
	i32 95, ; 118
	i32 132, ; 119
	i32 90, ; 120
	i32 41, ; 121
	i32 55, ; 122
	i32 121, ; 123
	i32 76, ; 124
	i32 115, ; 125
	i32 21, ; 126
	i32 132, ; 127
	i32 75, ; 128
	i32 21, ; 129
	i32 95, ; 130
	i32 31, ; 131
	i32 82, ; 132
	i32 58, ; 133
	i32 125, ; 134
	i32 62, ; 135
	i32 88, ; 136
	i32 134, ; 137
	i32 53, ; 138
	i32 6, ; 139
	i32 103, ; 140
	i32 116, ; 141
	i32 111, ; 142
	i32 51, ; 143
	i32 3, ; 144
	i32 46, ; 145
	i32 65, ; 146
	i32 85, ; 147
	i32 63, ; 148
	i32 1, ; 149
	i32 106, ; 150
	i32 48, ; 151
	i32 53, ; 152
	i32 12, ; 153
	i32 81, ; 154
	i32 15, ; 155
	i32 49, ; 156
	i32 64, ; 157
	i32 117, ; 158
	i32 56, ; 159
	i32 13, ; 160
	i32 81, ; 161
	i32 47, ; 162
	i32 9, ; 163
	i32 124, ; 164
	i32 56, ; 165
	i32 67, ; 166
	i32 72, ; 167
	i32 34, ; 168
	i32 60, ; 169
	i32 98, ; 170
	i32 14, ; 171
	i32 61, ; 172
	i32 122, ; 173
	i32 27, ; 174
	i32 50, ; 175
	i32 110, ; 176
	i32 1, ; 177
	i32 15, ; 178
	i32 36, ; 179
	i32 133, ; 180
	i32 9, ; 181
	i32 39, ; 182
	i32 84, ; 183
	i32 29, ; 184
	i32 30, ; 185
	i32 13, ; 186
	i32 77, ; 187
	i32 91, ; 188
	i32 8, ; 189
	i32 11, ; 190
	i32 94, ; 191
	i32 67, ; 192
	i32 3, ; 193
	i32 60, ; 194
	i32 128, ; 195
	i32 122, ; 196
	i32 107, ; 197
	i32 24, ; 198
	i32 5, ; 199
	i32 123, ; 200
	i32 38, ; 201
	i32 44, ; 202
	i32 16, ; 203
	i32 32, ; 204
	i32 39, ; 205
	i32 64, ; 206
	i32 33, ; 207
	i32 0, ; 208
	i32 45, ; 209
	i32 110, ; 210
	i32 98, ; 211
	i32 108, ; 212
	i32 102, ; 213
	i32 114, ; 214
	i32 17, ; 215
	i32 97, ; 216
	i32 100, ; 217
	i32 43, ; 218
	i32 63, ; 219
	i32 45, ; 220
	i32 94, ; 221
	i32 4, ; 222
	i32 109, ; 223
	i32 71, ; 224
	i32 130, ; 225
	i32 129, ; 226
	i32 4, ; 227
	i32 12, ; 228
	i32 5, ; 229
	i32 126, ; 230
	i32 105, ; 231
	i32 130, ; 232
	i32 36, ; 233
	i32 108, ; 234
	i32 18, ; 235
	i32 48, ; 236
	i32 46, ; 237
	i32 105, ; 238
	i32 40, ; 239
	i32 55, ; 240
	i32 76, ; 241
	i32 125, ; 242
	i32 25, ; 243
	i32 74, ; 244
	i32 88, ; 245
	i32 73, ; 246
	i32 91, ; 247
	i32 32, ; 248
	i32 126, ; 249
	i32 111, ; 250
	i32 10, ; 251
	i32 25, ; 252
	i32 112, ; 253
	i32 8, ; 254
	i32 35, ; 255
	i32 20, ; 256
	i32 37, ; 257
	i32 31, ; 258
	i32 72, ; 259
	i32 70, ; 260
	i32 117, ; 261
	i32 119, ; 262
	i32 128, ; 263
	i32 19, ; 264
	i32 73, ; 265
	i32 96, ; 266
	i32 87, ; 267
	i32 75, ; 268
	i32 14 ; 269
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 8

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 8

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 0, ; id 0x0; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 8

; Functions

; Function attributes: "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" uwtable willreturn
define void @xamarin_app_init(ptr nocapture noundef readnone %env, ptr noundef %fn) local_unnamed_addr #0
{
	%fnIsNull = icmp eq ptr %fn, null
	br i1 %fnIsNull, label %1, label %2

1: ; preds = %0
	%putsResult = call noundef i32 @puts(ptr @.str.0)
	call void @abort()
	unreachable 

2: ; preds = %1, %0
	store ptr %fn, ptr @get_function_pointer, align 8, !tbaa !3
	ret void
}

; Strings
@.str.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 1

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+fix-cortex-a53-835769,+neon,+outline-atomics,+v8a" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+fix-cortex-a53-835769,+neon,+outline-atomics,+v8a" }

; Metadata
!llvm.module.flags = !{!0, !1, !7, !8, !9, !10}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!"Xamarin.Android remotes/origin/release/8.0.4xx @ 82d8938cf80f6d5fa6c28529ddfbdb753d805ab4"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"branch-target-enforcement", i32 0}
!8 = !{i32 1, !"sign-return-address", i32 0}
!9 = !{i32 1, !"sign-return-address-all", i32 0}
!10 = !{i32 1, !"sign-return-address-with-bkey", i32 0}

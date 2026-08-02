; ModuleID = 'marshal_methods.x86.ll'
source_filename = "marshal_methods.x86.ll"
target datalayout = "e-m:e-p:32:32-p270:32:32-p271:32:32-p272:64:64-f64:32:64-f80:32-n8:16:32-S128"
target triple = "i686-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [135 x ptr] zeroinitializer, align 4

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [270 x i32] [
	i32 2616222, ; 0: System.Net.NetworkInformation.dll => 0x27eb9e => 105
	i32 10166715, ; 1: System.Net.NameResolution.dll => 0x9b21bb => 104
	i32 39485524, ; 2: System.Net.WebSockets.dll => 0x25a8054 => 112
	i32 42639949, ; 3: System.Threading.Thread => 0x28aa24d => 127
	i32 67008169, ; 4: zh-Hant\Microsoft.Maui.Controls.resources => 0x3fe76a9 => 33
	i32 72070932, ; 5: Microsoft.Maui.Graphics.dll => 0x44bb714 => 55
	i32 117431740, ; 6: System.Runtime.InteropServices => 0x6ffddbc => 117
	i32 122350210, ; 7: System.Threading.Channels.dll => 0x74aea82 => 126
	i32 142721839, ; 8: System.Net.WebHeaderCollection => 0x881c32f => 110
	i32 165246403, ; 9: Xamarin.AndroidX.Collection.dll => 0x9d975c3 => 61
	i32 182336117, ; 10: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0xade3a75 => 80
	i32 195452805, ; 11: vi/Microsoft.Maui.Controls.resources.dll => 0xba65f85 => 30
	i32 199333315, ; 12: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xbe195c3 => 31
	i32 205061960, ; 13: System.ComponentModel => 0xc38ff48 => 93
	i32 209917895, ; 14: MoraTuk.Mobile.dll => 0xc8317c7 => 86
	i32 221063263, ; 15: Microsoft.AspNetCore.Http.Connections.Client => 0xd2d285f => 36
	i32 280992041, ; 16: cs/Microsoft.Maui.Controls.resources.dll => 0x10bf9929 => 2
	i32 317674968, ; 17: vi\Microsoft.Maui.Controls.resources => 0x12ef55d8 => 30
	i32 318968648, ; 18: Xamarin.AndroidX.Activity.dll => 0x13031348 => 57
	i32 336156722, ; 19: ja/Microsoft.Maui.Controls.resources.dll => 0x14095832 => 15
	i32 342366114, ; 20: Xamarin.AndroidX.Lifecycle.Common => 0x146817a2 => 68
	i32 348048101, ; 21: Microsoft.AspNetCore.Http.Connections.Common.dll => 0x14becae5 => 37
	i32 356389973, ; 22: it/Microsoft.Maui.Controls.resources.dll => 0x153e1455 => 14
	i32 379916513, ; 23: System.Threading.Thread.dll => 0x16a510e1 => 127
	i32 385762202, ; 24: System.Memory.dll => 0x16fe439a => 101
	i32 395744057, ; 25: _Microsoft.Android.Resource.Designer => 0x17969339 => 34
	i32 435591531, ; 26: sv/Microsoft.Maui.Controls.resources.dll => 0x19f6996b => 26
	i32 442565967, ; 27: System.Collections => 0x1a61054f => 90
	i32 450948140, ; 28: Xamarin.AndroidX.Fragment.dll => 0x1ae0ec2c => 67
	i32 458494020, ; 29: Microsoft.AspNetCore.SignalR.Common.dll => 0x1b541044 => 40
	i32 469710990, ; 30: System.dll => 0x1bff388e => 130
	i32 498788369, ; 31: System.ObjectModel => 0x1dbae811 => 114
	i32 500358224, ; 32: id/Microsoft.Maui.Controls.resources.dll => 0x1dd2dc50 => 13
	i32 503918385, ; 33: fi/Microsoft.Maui.Controls.resources.dll => 0x1e092f31 => 7
	i32 513247710, ; 34: Microsoft.Extensions.Primitives.dll => 0x1e9789de => 50
	i32 539058512, ; 35: Microsoft.Extensions.Logging => 0x20216150 => 47
	i32 592146354, ; 36: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x234b6fb2 => 21
	i32 627609679, ; 37: Xamarin.AndroidX.CustomView => 0x2568904f => 65
	i32 627931235, ; 38: nl\Microsoft.Maui.Controls.resources => 0x256d7863 => 19
	i32 662205335, ; 39: System.Text.Encodings.Web.dll => 0x27787397 => 123
	i32 672442732, ; 40: System.Collections.Concurrent => 0x2814a96c => 87
	i32 683518922, ; 41: System.Net.Security => 0x28bdabca => 108
	i32 688181140, ; 42: ca/Microsoft.Maui.Controls.resources.dll => 0x2904cf94 => 1
	i32 706645707, ; 43: ko/Microsoft.Maui.Controls.resources.dll => 0x2a1e8ecb => 16
	i32 709557578, ; 44: de/Microsoft.Maui.Controls.resources.dll => 0x2a4afd4a => 4
	i32 722857257, ; 45: System.Runtime.Loader.dll => 0x2b15ed29 => 118
	i32 759454413, ; 46: System.Net.Requests => 0x2d445acd => 107
	i32 775507847, ; 47: System.IO.Compression => 0x2e394f87 => 98
	i32 777317022, ; 48: sk\Microsoft.Maui.Controls.resources => 0x2e54ea9e => 25
	i32 789151979, ; 49: Microsoft.Extensions.Options => 0x2f0980eb => 49
	i32 823281589, ; 50: System.Private.Uri.dll => 0x311247b5 => 115
	i32 830298997, ; 51: System.IO.Compression.Brotli => 0x317d5b75 => 97
	i32 832711436, ; 52: Microsoft.AspNetCore.SignalR.Protocols.Json.dll => 0x31a22b0c => 41
	i32 878954865, ; 53: System.Net.Http.Json => 0x3463c971 => 102
	i32 904024072, ; 54: System.ComponentModel.Primitives.dll => 0x35e25008 => 91
	i32 926902833, ; 55: tr/Microsoft.Maui.Controls.resources.dll => 0x373f6a31 => 28
	i32 967690846, ; 56: Xamarin.AndroidX.Lifecycle.Common.dll => 0x39adca5e => 68
	i32 992768348, ; 57: System.Collections.dll => 0x3b2c715c => 90
	i32 1012816738, ; 58: Xamarin.AndroidX.SavedState.dll => 0x3c5e5b62 => 78
	i32 1028951442, ; 59: Microsoft.Extensions.DependencyInjection.Abstractions => 0x3d548d92 => 45
	i32 1029334545, ; 60: da/Microsoft.Maui.Controls.resources.dll => 0x3d5a6611 => 3
	i32 1035644815, ; 61: Xamarin.AndroidX.AppCompat => 0x3dbaaf8f => 58
	i32 1044663988, ; 62: System.Linq.Expressions.dll => 0x3e444eb4 => 99
	i32 1052210849, ; 63: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x3eb776a1 => 70
	i32 1058641855, ; 64: Microsoft.AspNetCore.Http.Connections.Common => 0x3f1997bf => 37
	i32 1082857460, ; 65: System.ComponentModel.TypeConverter => 0x408b17f4 => 92
	i32 1084122840, ; 66: Xamarin.Kotlin.StdLib => 0x409e66d8 => 84
	i32 1098259244, ; 67: System => 0x41761b2c => 130
	i32 1118262833, ; 68: ko\Microsoft.Maui.Controls.resources => 0x42a75631 => 16
	i32 1168523401, ; 69: pt\Microsoft.Maui.Controls.resources => 0x45a64089 => 22
	i32 1178241025, ; 70: Xamarin.AndroidX.Navigation.Runtime.dll => 0x463a8801 => 75
	i32 1203215381, ; 71: pl/Microsoft.Maui.Controls.resources.dll => 0x47b79c15 => 20
	i32 1233093933, ; 72: Microsoft.AspNetCore.SignalR.Client.Core.dll => 0x497f852d => 39
	i32 1234928153, ; 73: nb/Microsoft.Maui.Controls.resources.dll => 0x499b8219 => 18
	i32 1260983243, ; 74: cs\Microsoft.Maui.Controls.resources => 0x4b2913cb => 2
	i32 1293217323, ; 75: Xamarin.AndroidX.DrawerLayout.dll => 0x4d14ee2b => 66
	i32 1324164729, ; 76: System.Linq => 0x4eed2679 => 100
	i32 1373134921, ; 77: zh-Hans\Microsoft.Maui.Controls.resources => 0x51d86049 => 32
	i32 1376866003, ; 78: Xamarin.AndroidX.SavedState => 0x52114ed3 => 78
	i32 1406073936, ; 79: Xamarin.AndroidX.CoordinatorLayout => 0x53cefc50 => 62
	i32 1414043276, ; 80: Microsoft.AspNetCore.Connections.Abstractions.dll => 0x5448968c => 35
	i32 1430672901, ; 81: ar\Microsoft.Maui.Controls.resources => 0x55465605 => 0
	i32 1452070440, ; 82: System.Formats.Asn1.dll => 0x568cd628 => 96
	i32 1458022317, ; 83: System.Net.Security.dll => 0x56e7a7ad => 108
	i32 1461004990, ; 84: es\Microsoft.Maui.Controls.resources => 0x57152abe => 6
	i32 1462112819, ; 85: System.IO.Compression.dll => 0x57261233 => 98
	i32 1469204771, ; 86: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x57924923 => 59
	i32 1470490898, ; 87: Microsoft.Extensions.Primitives => 0x57a5e912 => 50
	i32 1480492111, ; 88: System.IO.Compression.Brotli.dll => 0x583e844f => 97
	i32 1493001747, ; 89: hi/Microsoft.Maui.Controls.resources.dll => 0x58fd6613 => 10
	i32 1514721132, ; 90: el/Microsoft.Maui.Controls.resources.dll => 0x5a48cf6c => 5
	i32 1543031311, ; 91: System.Text.RegularExpressions.dll => 0x5bf8ca0f => 125
	i32 1551623176, ; 92: sk/Microsoft.Maui.Controls.resources.dll => 0x5c7be408 => 25
	i32 1618516317, ; 93: System.Net.WebSockets.Client.dll => 0x6078995d => 111
	i32 1622152042, ; 94: Xamarin.AndroidX.Loader.dll => 0x60b0136a => 72
	i32 1624863272, ; 95: Xamarin.AndroidX.ViewPager2 => 0x60d97228 => 82
	i32 1636350590, ; 96: Xamarin.AndroidX.CursorAdapter => 0x6188ba7e => 64
	i32 1639515021, ; 97: System.Net.Http.dll => 0x61b9038d => 103
	i32 1639986890, ; 98: System.Text.RegularExpressions => 0x61c036ca => 125
	i32 1657153582, ; 99: System.Runtime => 0x62c6282e => 120
	i32 1658251792, ; 100: Xamarin.Google.Android.Material.dll => 0x62d6ea10 => 83
	i32 1677501392, ; 101: System.Net.Primitives.dll => 0x63fca3d0 => 106
	i32 1678508291, ; 102: System.Net.WebSockets => 0x640c0103 => 112
	i32 1679769178, ; 103: System.Security.Cryptography => 0x641f3e5a => 121
	i32 1729485958, ; 104: Xamarin.AndroidX.CardView.dll => 0x6715dc86 => 60
	i32 1736233607, ; 105: ro/Microsoft.Maui.Controls.resources.dll => 0x677cd287 => 23
	i32 1743415430, ; 106: ca\Microsoft.Maui.Controls.resources => 0x67ea6886 => 1
	i32 1746115085, ; 107: System.IO.Pipelines.dll => 0x68139a0d => 56
	i32 1766324549, ; 108: Xamarin.AndroidX.SwipeRefreshLayout => 0x6947f945 => 80
	i32 1770582343, ; 109: Microsoft.Extensions.Logging.dll => 0x6988f147 => 47
	i32 1780572499, ; 110: Mono.Android.Runtime.dll => 0x6a216153 => 133
	i32 1782862114, ; 111: ms\Microsoft.Maui.Controls.resources => 0x6a445122 => 17
	i32 1788241197, ; 112: Xamarin.AndroidX.Fragment => 0x6a96652d => 67
	i32 1793755602, ; 113: he\Microsoft.Maui.Controls.resources => 0x6aea89d2 => 9
	i32 1808609942, ; 114: Xamarin.AndroidX.Loader => 0x6bcd3296 => 72
	i32 1813058853, ; 115: Xamarin.Kotlin.StdLib.dll => 0x6c111525 => 84
	i32 1813201214, ; 116: Xamarin.Google.Android.Material => 0x6c13413e => 83
	i32 1818569960, ; 117: Xamarin.AndroidX.Navigation.UI.dll => 0x6c652ce8 => 76
	i32 1824175904, ; 118: System.Text.Encoding.Extensions => 0x6cbab720 => 122
	i32 1828688058, ; 119: Microsoft.Extensions.Logging.Abstractions.dll => 0x6cff90ba => 48
	i32 1842015223, ; 120: uk/Microsoft.Maui.Controls.resources.dll => 0x6dcaebf7 => 29
	i32 1853025655, ; 121: sv\Microsoft.Maui.Controls.resources => 0x6e72ed77 => 26
	i32 1858542181, ; 122: System.Linq.Expressions => 0x6ec71a65 => 99
	i32 1875935024, ; 123: fr\Microsoft.Maui.Controls.resources => 0x6fd07f30 => 8
	i32 1910275211, ; 124: System.Collections.NonGeneric.dll => 0x71dc7c8b => 88
	i32 1945717188, ; 125: Microsoft.AspNetCore.SignalR.Client.Core => 0x73f949c4 => 39
	i32 1961813231, ; 126: Xamarin.AndroidX.Security.SecurityCrypto.dll => 0x74eee4ef => 79
	i32 1967334205, ; 127: Microsoft.AspNetCore.SignalR.Common => 0x7543233d => 40
	i32 1968388702, ; 128: Microsoft.Extensions.Configuration.dll => 0x75533a5e => 42
	i32 2003115576, ; 129: el\Microsoft.Maui.Controls.resources => 0x77651e38 => 5
	i32 2019465201, ; 130: Xamarin.AndroidX.Lifecycle.ViewModel => 0x785e97f1 => 70
	i32 2025202353, ; 131: ar/Microsoft.Maui.Controls.resources.dll => 0x78b622b1 => 0
	i32 2045470958, ; 132: System.Private.Xml => 0x79eb68ee => 116
	i32 2055257422, ; 133: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x7a80bd4e => 69
	i32 2066184531, ; 134: de\Microsoft.Maui.Controls.resources => 0x7b277953 => 4
	i32 2079903147, ; 135: System.Runtime.dll => 0x7bf8cdab => 120
	i32 2090596640, ; 136: System.Numerics.Vectors => 0x7c9bf920 => 113
	i32 2127167465, ; 137: System.Console => 0x7ec9ffe9 => 94
	i32 2142473426, ; 138: System.Collections.Specialized => 0x7fb38cd2 => 89
	i32 2159891885, ; 139: Microsoft.Maui => 0x80bd55ad => 53
	i32 2169148018, ; 140: hu\Microsoft.Maui.Controls.resources => 0x814a9272 => 12
	i32 2181898931, ; 141: Microsoft.Extensions.Options.dll => 0x820d22b3 => 49
	i32 2192057212, ; 142: Microsoft.Extensions.Logging.Abstractions => 0x82a8237c => 48
	i32 2193016926, ; 143: System.ObjectModel.dll => 0x82b6c85e => 114
	i32 2201107256, ; 144: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x83323b38 => 85
	i32 2201231467, ; 145: System.Net.Http => 0x8334206b => 103
	i32 2207618523, ; 146: it\Microsoft.Maui.Controls.resources => 0x839595db => 14
	i32 2229158877, ; 147: Microsoft.Extensions.Features.dll => 0x84de43dd => 46
	i32 2266799131, ; 148: Microsoft.Extensions.Configuration.Abstractions => 0x871c9c1b => 43
	i32 2270573516, ; 149: fr/Microsoft.Maui.Controls.resources.dll => 0x875633cc => 8
	i32 2279755925, ; 150: Xamarin.AndroidX.RecyclerView.dll => 0x87e25095 => 77
	i32 2295906218, ; 151: System.Net.Sockets => 0x88d8bfaa => 109
	i32 2303942373, ; 152: nb\Microsoft.Maui.Controls.resources => 0x89535ee5 => 18
	i32 2305521784, ; 153: System.Private.CoreLib.dll => 0x896b7878 => 131
	i32 2319144366, ; 154: Microsoft.AspNetCore.SignalR.Client => 0x8a3b55ae => 38
	i32 2353062107, ; 155: System.Net.Primitives => 0x8c40e0db => 106
	i32 2368005991, ; 156: System.Xml.ReaderWriter.dll => 0x8d24e767 => 129
	i32 2371007202, ; 157: Microsoft.Extensions.Configuration => 0x8d52b2e2 => 42
	i32 2395872292, ; 158: id\Microsoft.Maui.Controls.resources => 0x8ece1c24 => 13
	i32 2427813419, ; 159: hi\Microsoft.Maui.Controls.resources => 0x90b57e2b => 10
	i32 2435356389, ; 160: System.Console.dll => 0x912896e5 => 94
	i32 2458678730, ; 161: System.Net.Sockets.dll => 0x928c75ca => 109
	i32 2475788418, ; 162: Java.Interop.dll => 0x93918882 => 132
	i32 2480646305, ; 163: Microsoft.Maui.Controls => 0x93dba8a1 => 51
	i32 2550873716, ; 164: hr\Microsoft.Maui.Controls.resources => 0x980b3e74 => 11
	i32 2570120770, ; 165: System.Text.Encodings.Web => 0x9930ee42 => 123
	i32 2585220780, ; 166: System.Text.Encoding.Extensions.dll => 0x9a1756ac => 122
	i32 2593496499, ; 167: pl\Microsoft.Maui.Controls.resources => 0x9a959db3 => 20
	i32 2605712449, ; 168: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x9b500441 => 85
	i32 2617129537, ; 169: System.Private.Xml.dll => 0x9bfe3a41 => 116
	i32 2620871830, ; 170: Xamarin.AndroidX.CursorAdapter.dll => 0x9c375496 => 64
	i32 2626831493, ; 171: ja\Microsoft.Maui.Controls.resources => 0x9c924485 => 15
	i32 2637500010, ; 172: Microsoft.Extensions.Features => 0x9d350e6a => 46
	i32 2663698177, ; 173: System.Runtime.Loader => 0x9ec4cf01 => 118
	i32 2724373263, ; 174: System.Runtime.Numerics.dll => 0xa262a30f => 119
	i32 2732626843, ; 175: Xamarin.AndroidX.Activity => 0xa2e0939b => 57
	i32 2735172069, ; 176: System.Threading.Channels => 0xa30769e5 => 126
	i32 2737747696, ; 177: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0xa32eb6f0 => 59
	i32 2752995522, ; 178: pt-BR\Microsoft.Maui.Controls.resources => 0xa41760c2 => 21
	i32 2758225723, ; 179: Microsoft.Maui.Controls.Xaml => 0xa4672f3b => 52
	i32 2764765095, ; 180: Microsoft.Maui.dll => 0xa4caf7a7 => 53
	i32 2778768386, ; 181: Xamarin.AndroidX.ViewPager.dll => 0xa5a0a402 => 81
	i32 2785988530, ; 182: th\Microsoft.Maui.Controls.resources => 0xa60ecfb2 => 27
	i32 2801831435, ; 183: Microsoft.Maui.Graphics => 0xa7008e0b => 55
	i32 2806116107, ; 184: es/Microsoft.Maui.Controls.resources.dll => 0xa741ef0b => 6
	i32 2810250172, ; 185: Xamarin.AndroidX.CoordinatorLayout.dll => 0xa78103bc => 62
	i32 2831556043, ; 186: nl/Microsoft.Maui.Controls.resources.dll => 0xa8c61dcb => 19
	i32 2853208004, ; 187: Xamarin.AndroidX.ViewPager => 0xaa107fc4 => 81
	i32 2861189240, ; 188: Microsoft.Maui.Essentials => 0xaa8a4878 => 54
	i32 2875347124, ; 189: Microsoft.AspNetCore.Http.Connections.Client.dll => 0xab6250b4 => 36
	i32 2909740682, ; 190: System.Private.CoreLib => 0xad6f1e8a => 131
	i32 2916838712, ; 191: Xamarin.AndroidX.ViewPager2.dll => 0xaddb6d38 => 82
	i32 2919462931, ; 192: System.Numerics.Vectors.dll => 0xae037813 => 113
	i32 2959614098, ; 193: System.ComponentModel.dll => 0xb0682092 => 93
	i32 2978675010, ; 194: Xamarin.AndroidX.DrawerLayout => 0xb18af942 => 66
	i32 2987532451, ; 195: Xamarin.AndroidX.Security.SecurityCrypto => 0xb21220a3 => 79
	i32 2988600919, ; 196: MoraTuk.Mobile => 0xb2226e57 => 86
	i32 3038032645, ; 197: _Microsoft.Android.Resource.Designer.dll => 0xb514b305 => 34
	i32 3057625584, ; 198: Xamarin.AndroidX.Navigation.Common => 0xb63fa9f0 => 73
	i32 3059408633, ; 199: Mono.Android.Runtime => 0xb65adef9 => 133
	i32 3059793426, ; 200: System.ComponentModel.Primitives => 0xb660be12 => 91
	i32 3077302341, ; 201: hu/Microsoft.Maui.Controls.resources.dll => 0xb76be845 => 12
	i32 3103600923, ; 202: System.Formats.Asn1 => 0xb8fd311b => 96
	i32 3178803400, ; 203: Xamarin.AndroidX.Navigation.Fragment.dll => 0xbd78b0c8 => 74
	i32 3220365878, ; 204: System.Threading => 0xbff2e236 => 128
	i32 3258312781, ; 205: Xamarin.AndroidX.CardView => 0xc235e84d => 60
	i32 3305363605, ; 206: fi\Microsoft.Maui.Controls.resources => 0xc503d895 => 7
	i32 3316684772, ; 207: System.Net.Requests.dll => 0xc5b097e4 => 107
	i32 3317135071, ; 208: Xamarin.AndroidX.CustomView.dll => 0xc5b776df => 65
	i32 3346324047, ; 209: Xamarin.AndroidX.Navigation.Runtime => 0xc774da4f => 75
	i32 3357674450, ; 210: ru\Microsoft.Maui.Controls.resources => 0xc8220bd2 => 24
	i32 3358260929, ; 211: System.Text.Json => 0xc82afec1 => 124
	i32 3362522851, ; 212: Xamarin.AndroidX.Core => 0xc86c06e3 => 63
	i32 3366347497, ; 213: Java.Interop => 0xc8a662e9 => 132
	i32 3374999561, ; 214: Xamarin.AndroidX.RecyclerView => 0xc92a6809 => 77
	i32 3381016424, ; 215: da\Microsoft.Maui.Controls.resources => 0xc9863768 => 3
	i32 3428513518, ; 216: Microsoft.Extensions.DependencyInjection.dll => 0xcc5af6ee => 44
	i32 3463511458, ; 217: hr/Microsoft.Maui.Controls.resources.dll => 0xce70fda2 => 11
	i32 3466904072, ; 218: Microsoft.AspNetCore.SignalR.Client.dll => 0xcea4c208 => 38
	i32 3471940407, ; 219: System.ComponentModel.TypeConverter.dll => 0xcef19b37 => 92
	i32 3476120550, ; 220: Mono.Android => 0xcf3163e6 => 134
	i32 3479583265, ; 221: ru/Microsoft.Maui.Controls.resources.dll => 0xcf663a21 => 24
	i32 3484440000, ; 222: ro\Microsoft.Maui.Controls.resources => 0xcfb055c0 => 23
	i32 3485117614, ; 223: System.Text.Json.dll => 0xcfbaacae => 124
	i32 3580758918, ; 224: zh-HK\Microsoft.Maui.Controls.resources => 0xd56e0b86 => 31
	i32 3598340787, ; 225: System.Net.WebSockets.Client => 0xd67a52b3 => 111
	i32 3608519521, ; 226: System.Linq.dll => 0xd715a361 => 100
	i32 3641597786, ; 227: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0xd90e5f5a => 69
	i32 3643446276, ; 228: tr\Microsoft.Maui.Controls.resources => 0xd92a9404 => 28
	i32 3643854240, ; 229: Xamarin.AndroidX.Navigation.Fragment => 0xd930cda0 => 74
	i32 3657292374, ; 230: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd9fdda56 => 43
	i32 3660523487, ; 231: System.Net.NetworkInformation => 0xda2f27df => 105
	i32 3672681054, ; 232: Mono.Android.dll => 0xdae8aa5e => 134
	i32 3691870036, ; 233: Microsoft.AspNetCore.SignalR.Protocols.Json => 0xdc0d7754 => 41
	i32 3697841164, ; 234: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xdc68940c => 33
	i32 3724971120, ; 235: Xamarin.AndroidX.Navigation.Common.dll => 0xde068c70 => 73
	i32 3732100267, ; 236: System.Net.NameResolution => 0xde7354ab => 104
	i32 3737834244, ; 237: System.Net.Http.Json.dll => 0xdecad304 => 102
	i32 3748608112, ; 238: System.Diagnostics.DiagnosticSource => 0xdf6f3870 => 95
	i32 3786282454, ; 239: Xamarin.AndroidX.Collection => 0xe1ae15d6 => 61
	i32 3787005001, ; 240: Microsoft.AspNetCore.Connections.Abstractions => 0xe1b91c49 => 35
	i32 3792276235, ; 241: System.Collections.NonGeneric => 0xe2098b0b => 88
	i32 3802395368, ; 242: System.Collections.Specialized.dll => 0xe2a3f2e8 => 89
	i32 3823082795, ; 243: System.Security.Cryptography.dll => 0xe3df9d2b => 121
	i32 3841636137, ; 244: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xe4fab729 => 45
	i32 3849253459, ; 245: System.Runtime.InteropServices.dll => 0xe56ef253 => 117
	i32 3885497537, ; 246: System.Net.WebHeaderCollection.dll => 0xe797fcc1 => 110
	i32 3889960447, ; 247: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xe7dc15ff => 32
	i32 3896106733, ; 248: System.Collections.Concurrent.dll => 0xe839deed => 87
	i32 3896760992, ; 249: Xamarin.AndroidX.Core.dll => 0xe843daa0 => 63
	i32 3928044579, ; 250: System.Xml.ReaderWriter => 0xea213423 => 129
	i32 3931092270, ; 251: Xamarin.AndroidX.Navigation.UI => 0xea4fb52e => 76
	i32 3955647286, ; 252: Xamarin.AndroidX.AppCompat.dll => 0xebc66336 => 58
	i32 3980434154, ; 253: th/Microsoft.Maui.Controls.resources.dll => 0xed409aea => 27
	i32 3987592930, ; 254: he/Microsoft.Maui.Controls.resources.dll => 0xedadd6e2 => 9
	i32 4023392905, ; 255: System.IO.Pipelines => 0xefd01a89 => 56
	i32 4025784931, ; 256: System.Memory => 0xeff49a63 => 101
	i32 4046471985, ; 257: Microsoft.Maui.Controls.Xaml.dll => 0xf1304331 => 52
	i32 4073602200, ; 258: System.Threading.dll => 0xf2ce3c98 => 128
	i32 4094352644, ; 259: Microsoft.Maui.Essentials.dll => 0xf40add04 => 54
	i32 4100113165, ; 260: System.Private.Uri => 0xf462c30d => 115
	i32 4102112229, ; 261: pt/Microsoft.Maui.Controls.resources.dll => 0xf48143e5 => 22
	i32 4125707920, ; 262: ms/Microsoft.Maui.Controls.resources.dll => 0xf5e94e90 => 17
	i32 4126470640, ; 263: Microsoft.Extensions.DependencyInjection => 0xf5f4f1f0 => 44
	i32 4150914736, ; 264: uk\Microsoft.Maui.Controls.resources => 0xf769eeb0 => 29
	i32 4182413190, ; 265: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0xf94a8f86 => 71
	i32 4213026141, ; 266: System.Diagnostics.DiagnosticSource.dll => 0xfb1dad5d => 95
	i32 4271975918, ; 267: Microsoft.Maui.Controls.dll => 0xfea12dee => 51
	i32 4274976490, ; 268: System.Runtime.Numerics => 0xfecef6ea => 119
	i32 4292120959 ; 269: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xffd4917f => 71
], align 4

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [270 x i32] [
	i32 105, ; 0
	i32 104, ; 1
	i32 112, ; 2
	i32 127, ; 3
	i32 33, ; 4
	i32 55, ; 5
	i32 117, ; 6
	i32 126, ; 7
	i32 110, ; 8
	i32 61, ; 9
	i32 80, ; 10
	i32 30, ; 11
	i32 31, ; 12
	i32 93, ; 13
	i32 86, ; 14
	i32 36, ; 15
	i32 2, ; 16
	i32 30, ; 17
	i32 57, ; 18
	i32 15, ; 19
	i32 68, ; 20
	i32 37, ; 21
	i32 14, ; 22
	i32 127, ; 23
	i32 101, ; 24
	i32 34, ; 25
	i32 26, ; 26
	i32 90, ; 27
	i32 67, ; 28
	i32 40, ; 29
	i32 130, ; 30
	i32 114, ; 31
	i32 13, ; 32
	i32 7, ; 33
	i32 50, ; 34
	i32 47, ; 35
	i32 21, ; 36
	i32 65, ; 37
	i32 19, ; 38
	i32 123, ; 39
	i32 87, ; 40
	i32 108, ; 41
	i32 1, ; 42
	i32 16, ; 43
	i32 4, ; 44
	i32 118, ; 45
	i32 107, ; 46
	i32 98, ; 47
	i32 25, ; 48
	i32 49, ; 49
	i32 115, ; 50
	i32 97, ; 51
	i32 41, ; 52
	i32 102, ; 53
	i32 91, ; 54
	i32 28, ; 55
	i32 68, ; 56
	i32 90, ; 57
	i32 78, ; 58
	i32 45, ; 59
	i32 3, ; 60
	i32 58, ; 61
	i32 99, ; 62
	i32 70, ; 63
	i32 37, ; 64
	i32 92, ; 65
	i32 84, ; 66
	i32 130, ; 67
	i32 16, ; 68
	i32 22, ; 69
	i32 75, ; 70
	i32 20, ; 71
	i32 39, ; 72
	i32 18, ; 73
	i32 2, ; 74
	i32 66, ; 75
	i32 100, ; 76
	i32 32, ; 77
	i32 78, ; 78
	i32 62, ; 79
	i32 35, ; 80
	i32 0, ; 81
	i32 96, ; 82
	i32 108, ; 83
	i32 6, ; 84
	i32 98, ; 85
	i32 59, ; 86
	i32 50, ; 87
	i32 97, ; 88
	i32 10, ; 89
	i32 5, ; 90
	i32 125, ; 91
	i32 25, ; 92
	i32 111, ; 93
	i32 72, ; 94
	i32 82, ; 95
	i32 64, ; 96
	i32 103, ; 97
	i32 125, ; 98
	i32 120, ; 99
	i32 83, ; 100
	i32 106, ; 101
	i32 112, ; 102
	i32 121, ; 103
	i32 60, ; 104
	i32 23, ; 105
	i32 1, ; 106
	i32 56, ; 107
	i32 80, ; 108
	i32 47, ; 109
	i32 133, ; 110
	i32 17, ; 111
	i32 67, ; 112
	i32 9, ; 113
	i32 72, ; 114
	i32 84, ; 115
	i32 83, ; 116
	i32 76, ; 117
	i32 122, ; 118
	i32 48, ; 119
	i32 29, ; 120
	i32 26, ; 121
	i32 99, ; 122
	i32 8, ; 123
	i32 88, ; 124
	i32 39, ; 125
	i32 79, ; 126
	i32 40, ; 127
	i32 42, ; 128
	i32 5, ; 129
	i32 70, ; 130
	i32 0, ; 131
	i32 116, ; 132
	i32 69, ; 133
	i32 4, ; 134
	i32 120, ; 135
	i32 113, ; 136
	i32 94, ; 137
	i32 89, ; 138
	i32 53, ; 139
	i32 12, ; 140
	i32 49, ; 141
	i32 48, ; 142
	i32 114, ; 143
	i32 85, ; 144
	i32 103, ; 145
	i32 14, ; 146
	i32 46, ; 147
	i32 43, ; 148
	i32 8, ; 149
	i32 77, ; 150
	i32 109, ; 151
	i32 18, ; 152
	i32 131, ; 153
	i32 38, ; 154
	i32 106, ; 155
	i32 129, ; 156
	i32 42, ; 157
	i32 13, ; 158
	i32 10, ; 159
	i32 94, ; 160
	i32 109, ; 161
	i32 132, ; 162
	i32 51, ; 163
	i32 11, ; 164
	i32 123, ; 165
	i32 122, ; 166
	i32 20, ; 167
	i32 85, ; 168
	i32 116, ; 169
	i32 64, ; 170
	i32 15, ; 171
	i32 46, ; 172
	i32 118, ; 173
	i32 119, ; 174
	i32 57, ; 175
	i32 126, ; 176
	i32 59, ; 177
	i32 21, ; 178
	i32 52, ; 179
	i32 53, ; 180
	i32 81, ; 181
	i32 27, ; 182
	i32 55, ; 183
	i32 6, ; 184
	i32 62, ; 185
	i32 19, ; 186
	i32 81, ; 187
	i32 54, ; 188
	i32 36, ; 189
	i32 131, ; 190
	i32 82, ; 191
	i32 113, ; 192
	i32 93, ; 193
	i32 66, ; 194
	i32 79, ; 195
	i32 86, ; 196
	i32 34, ; 197
	i32 73, ; 198
	i32 133, ; 199
	i32 91, ; 200
	i32 12, ; 201
	i32 96, ; 202
	i32 74, ; 203
	i32 128, ; 204
	i32 60, ; 205
	i32 7, ; 206
	i32 107, ; 207
	i32 65, ; 208
	i32 75, ; 209
	i32 24, ; 210
	i32 124, ; 211
	i32 63, ; 212
	i32 132, ; 213
	i32 77, ; 214
	i32 3, ; 215
	i32 44, ; 216
	i32 11, ; 217
	i32 38, ; 218
	i32 92, ; 219
	i32 134, ; 220
	i32 24, ; 221
	i32 23, ; 222
	i32 124, ; 223
	i32 31, ; 224
	i32 111, ; 225
	i32 100, ; 226
	i32 69, ; 227
	i32 28, ; 228
	i32 74, ; 229
	i32 43, ; 230
	i32 105, ; 231
	i32 134, ; 232
	i32 41, ; 233
	i32 33, ; 234
	i32 73, ; 235
	i32 104, ; 236
	i32 102, ; 237
	i32 95, ; 238
	i32 61, ; 239
	i32 35, ; 240
	i32 88, ; 241
	i32 89, ; 242
	i32 121, ; 243
	i32 45, ; 244
	i32 117, ; 245
	i32 110, ; 246
	i32 32, ; 247
	i32 87, ; 248
	i32 63, ; 249
	i32 129, ; 250
	i32 76, ; 251
	i32 58, ; 252
	i32 27, ; 253
	i32 9, ; 254
	i32 56, ; 255
	i32 101, ; 256
	i32 52, ; 257
	i32 128, ; 258
	i32 54, ; 259
	i32 115, ; 260
	i32 22, ; 261
	i32 17, ; 262
	i32 44, ; 263
	i32 29, ; 264
	i32 71, ; 265
	i32 95, ; 266
	i32 51, ; 267
	i32 119, ; 268
	i32 71 ; 269
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 4

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 4

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 0, ; id 0x0; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 4

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
	store ptr %fn, ptr @get_function_pointer, align 4, !tbaa !3
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
attributes #0 = { "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" "stackrealign" "target-cpu"="i686" "target-features"="+cx8,+mmx,+sse,+sse2,+sse3,+ssse3,+x87" "tune-cpu"="generic" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8" "stackrealign" "target-cpu"="i686" "target-features"="+cx8,+mmx,+sse,+sse2,+sse3,+ssse3,+x87" "tune-cpu"="generic" }

; Metadata
!llvm.module.flags = !{!0, !1, !7}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!"Xamarin.Android remotes/origin/release/8.0.4xx @ 82d8938cf80f6d5fa6c28529ddfbdb753d805ab4"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"NumRegisterParameters", i32 0}

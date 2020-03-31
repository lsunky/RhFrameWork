描述:本框架为一个空壳unity工程，包含热更新方案，editor工具
## Editor RhMenu类
### preAssetBundleConfig
配置assetbundle打包规则，后缀。以下为四种打bundle规则

* None 零碎打包
* AllToOne 所有的子文件全打到根目录这一个包里
* FloderToOne 各自文件夹下批量打到一个包里
* DIY 自定义规则

### Simulation Mode
是否模拟bundle,如果是，则不需要每次打assetBundle，而是通过AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName接口得到资源路径，并通过AssetDatabase.LoadAssetAtPath 加载资源，只有在editor环境有用。
如果选否，则每次要打包assetBundle，读取正式资源。

### ResUpdate
是否开启资源热更，如果是则走apk同样的流程，方便查问题。如果否，则不进行热更新。

### 写入bundle名
按照指定打包配置，把不同文件夹下文件写入对应assetbunle标签，同时生成assetbundle名与路径名的映射表FilePath2BundleList，这样业务层在用的时候只需要关心资源路径，根本不需要关心bundle打包规则。

### CreateBundle
创建新的assetbundle,并删除无用的bundle。

### GenerateFileList
构建文件列表（files.txt），并对VersionConfig升级，同时写入文件列表的md5

### CreateChangList
根据上次的files.txt和本次生成的files.txt做比对，生成改变列表，列出增加文件，删除文件以及改变文件，方便看到热更列表。

## 游戏启动
Lanch为游戏的初始入口,调用状态机初始化，

## GameState游戏状态

### CheckAppVersion
根据云上的资源列表，检查本机的版本是否需要下载新包，如果是则弹窗提示，如果不是则进入下一阶段。

### CopyToPersistent

解包，检查本包是否第一次运行，如果是则把包内streamingAssets资源copy到可热更目录（PersistentPath），本demo没有用多线程，如果对效率有要求可以借用多线程。

### CheckResource

比对资源，根据云上的文件列表，和本地文件列表做比对，得到两个版本的改变列表。

### DownResource

下载资源，根据得到的差异列表，看本地文件是否已经更新到最新的资源（防止上次更新了一些，后面杀进程），如果是则跳过，如果不是则以此更新。

### GameMain

游戏的真正模块，一般可以在此打开登录页面。























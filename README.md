<h3>详细介绍：https://blog.csdn.net/qq_33700123/article/details/113835834</h3> 

<h2>系统使用教程</h2> 

智能化工具作用是方便有效的管理每个HLOD System，如图。每个脚本都管理着一个系统，脚本引用着当前系统精细度模型、BVH、合批模型的跟节点。 <br />
 <img src="/DocumentationImages/1.png" width="50%">   

 <h3>工具支持</h3> 
1、一键生成、删除
2、一键更新
3、贴图、模型、流式资源管理
4、其他功能
5、流式加载

 <h4>一键生成、删除</h4> 
一键生成、删除功能如图，生成的节点名字后缀是一个递增的数字。<br />
 <img src="/DocumentationImages/2.png" width="50%">   
 
 <h4>一键更新</h4> 
一键更新功能如图4-3所示，当某个区域增加或删除了LOD Group时，使用更新功能只会更新有影响的部分，更新的包括网格和贴图以及保存在Asset下的资源。 <br />
 <img src="/DocumentationImages/3.png" width="50%">   
 
 <h4>贴图、模型、流式资源管理</h4> 
如图，Textures、Mesh（fbx文件）保存路径可自定义，导出的时候命名会与节点的名字一样 <br />
 <img src="/DocumentationImages/4.png" width="50%">   
 
如图，流式资源路径可自定义，拥有导出和退回功能。 <br />
 <img src="/DocumentationImages/5_1.png" width="50%">   
 <img src="/DocumentationImages/5_2.png" width="50%">  
 
 <h4>其他功能</h4> 
1.BVH划分条件
Maximun Layer：生成最大有效层级（从底往上）
Cull：开启条件剔除
Bound Condtion Dia：剔除包围盒最大直径超过指定大小
Calbound Of Child：包围盒计算计算子节点 <br />
 <img src="/DocumentationImages/6.png" width="50%">   
 
BVH划分条件
2.HLOD Cull运行相关
DelayUnLoadTime：流式延时卸载
GI CacheDistance：缓冲计算距离 <br />
 <img src="/DocumentationImages/7.png" width="50%">   
 
<h3>流式加载</h3> 
注意：LodGroup必须式预制体，合并生成出来的预制体和原有的预制体必须添加在Addressables上才能正常运行，并在场景将ActionLoadAssetPool挂载上

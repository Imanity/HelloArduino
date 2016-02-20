## SRT现状

### 仓库说明


- HelloAndroid 文件夹下为Android工程
 - 编译方式：在Android studio自带console中运行:
 ``` bash
 gradlew makeJar
 ```
 生成的jar库文件在app/build/libs/目录下
 - 注意:若console无法输入,请在win10自带cmd中使用选项:使用旧版控制台并重启电脑
 - 第一次编译需要翻墙下载,需在Android studio中设置代理
- ArduinoFinal 文件夹下为Arduino工程
 - Master 为连接蓝牙的Arduino nano所用
 - Slave 为另一Arduino nano所用
- HelloUnity 文件夹下为Unity工程
 - Unity版本不同直接clone可能无法正常打开
 - 暂无解决方案
- exec 文件夹中为生成的apk,可导入安卓调试
- datasheets 文件夹中为Arduino所用芯片使用文档
- libraries 文件夹中为Arduino工程编译所需库

_其余文件为测试文件,非项目必需_
- StickerMan 文件夹下为Android工程，肢解的火柴人

### 进度

已完成：在unity中显示一个人,上肢可动

下一步：制作 demo 用具，形态可能是一件外套

### 尚存的问题
- Arduino端: 真机测试感觉数据失真严重,且延迟较大,可能为磁场相互干扰所致,待demo用具准备完毕相互距离增大后需检验结果正确性
- Android端: 蓝牙掉线重连功能尚未实现(optional)
- Unity端: 人物胳膊在某些情况下严重扭曲(弃疗,不太可能修复)

### 时间表

- 2-22 制作demo用具并进行测试
- 2-22 开始准备合并工作

### 经费使用情况

#### 目前由唐人杰垫付
- [虚拟现实头盔Google Cardboard VR 手机3D眼镜暴风魔镜 谷歌纸盒](https://item.taobao.com/item.htm?spm=a1z09.2.0.0.DjCSlq&id=520234337257&_u=n2brmaj1657d) * 1 ¥9.90
- MOKE手机VR魔镜暴风虚拟现实3D眼镜手机头戴式游戏头盔3代电影BOX(已下架) * 1 ¥168.00
- [MPU9250 9DOF 九轴/9轴姿态 加速度 陀螺仪 指南针 磁场传感器](https://detail.tmall.com/item.htm?id=42322982187) * 5 ¥172.34
- [云辉 arduino nano V3.0 ATMEGA328P 电子积木 互动媒体](https://detail.tmall.com/item.htm?id=40698124597) * 4 ¥62.50
- [螃蟹王国 DIY科技 蓝牙模块Arduino 蓝牙串口 HC-06 蓝牙串口模块](https://detail.tmall.com/item.htm?id=38213300612) * 1 ¥39.00
- arduino uno r3 arduino入门套件 arduino初学者学习套件 开发板(已下架) * 1 ¥136.00
- Arduino UNO R3 单片机开发板1号 改进版行家版创客入门初学套件(已下架) * 1 ¥30.00
- [USBT型口充电线 数据线 5pin平板MP3硬盘相机汽车导航数据线](https://detail.tmall.com/item.htm?id=525471590592) * 1 ¥18.00
- [易之力电烙铁高级恒温内热式电烙铁家用精密焊接电子维修烙铁套装](https://detail.tmall.com/item.htm?id=525146221372) * 1 ¥62.00
- [杜邦线 40P彩色 母对母 公对母 公对公 10/15/20/30/21CM](https://detail.tmall.com/item.htm?id=21555044507) * 1 ¥7.36

以上各项总计 ¥705.10

#### 目前由李肇阳垫付
- [Arduino UNO 3 套件](https://item.taobao.com/item.htm?id=40407396235 "arduino uno r3 arduino入门套件 arduino初学者学习套件 开发板") * 1 ¥136.00
- [HC-06 蓝牙模块](https://item.taobao.com/item.htm?id=41265336336 "HC-06 无线蓝牙串口透传模块 无线串口通讯 HC-06从机模块") * 1 ¥16.00
- [MPU-9250 九轴传感器模块](https://item.taobao.com/item.htm?id=42408784668 "磁场MPU9250 9DOF 九轴/9轴姿态 加速度 陀螺仪 指南针磁场传感器") * 5 ¥159.42
- [Arduino nano](https://detail.tmall.com/item.htm?id=522223298784 "LANGUO Arduino nano V3.0 ATMEGA328P 改进版 无焊板 无配线") * 3 ¥36.6
- [彩色杜邦线 40p 20cm](https://detail.tmall.com/item.htm?id=45612590918 "公对母、公对公、母对母各一件") 共 120 根，¥10.4

以上各项，加运费共 ¥15，总计 ¥373.42

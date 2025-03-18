# BMS to osu!mania 7k

> 将 BMS 转换为 osu! mania 7k / 7+1k 谱面

## 用法

### 命令行参数（推荐）

```
  -i, --input       Required. 输入文件夹，程序将在输入文件夹里递归查找可用的BMS谱面文件，可用的文件包括 .bms/.bml/.bme/.bmx
  -o, --output      Required. 输出文件夹（输出文件夹将保持和输入文件夹相同的目录结构）
  --no-sv           是否包含 SV（即是否包含变速），默认包含
  --no-zip          是否将输出文件夹压缩成 .osz，默认压缩
  --no-copy         是否将 bms 谱面中的 声音/bga/图片 文件复制到输出文件夹，默认复制
  --no-remove       是否在将输出文件夹压缩成 .osz 后删除输出文件夹，默认删除
  --generate-mp3    是否从 bms 的采样中生成完整 mp3 文件，默认不生成（需要有 ffmpeg）
  --ffmpeg          ffmpeg 可执行文件的路径，不指定则会在 PATH 中找
  --max-threads     ffmpeg 的最大调用线程数，默认 10（过大会导致在上下文切换中浪费大量的时间）
  --help            展示帮助信息（但是英文）
```

**值得注意的是**，输入文件夹可以包含多个歌，输出问价夹会解析所有的歌曲，如

```
.\BmsToOsu.exe -i "O:\BMS Song Pack\multi" -o aaa --no-zip
```

其中
```
O:\BMS Song Pack\multi
├── song1
│   ├── 1.bms
│   └── 2.bms
└── song2
    ├── 1.bms
    └── 2.bms
```

则输出文件夹是
```
aaa
├── song1
│   ├── 1.osu
│   ├── 1(7+1k).osu
│   ├── 2.osu
│   ├── ...
│   └── 2(7+1k).osu
└── song2
    ├── 1.osu
    ├── ...
    └── 2.osu
```

这时候建议使用 `--no-zip` 参数，否则程序会将整个`aaa`打包成一个 `osz` 文件

### 命令行 GUI

如果不指定任何启动参数，程序将启动一个命令行 GUI，其中的选项和命令行参数对应：

[![image.png](https://i.postimg.cc/zDbtkwpx/image.png)](https://postimg.cc/hhn1Ddkm)

### 示例

#### 输入包含单个歌曲

```
$ .\BmsToOsu.exe \
    -i "O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆" \
    -o aaa --generate-mp3
```


<details>
<summary>
输出
</summary>

```
|Info|Use FFMPEG: C:\bin\ffmpeg.exe
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_b7k.bme
|Warn|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_b7k.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_fz5.bms
|Warn|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_fz5.bms: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_fz7.bms
|Warn|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_fz7.bms: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je10.bms
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je10.bms: Double play mode; skipping
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je5.bms
|Warn|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je5.bms: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_nm7.bme
|Warn|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_nm7.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_nm7a.bme
|Warn|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_nm7a.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_plus_system.bme
|Warn|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_plus_system.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_sla7.bme
|Warn|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_sla7.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_TML7.bml
|Warn|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_TML7.bml: Bga frame 8a is not founded, ignoring...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:01:18.7499999-00:01:24.2045454...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:34.6022727-00:00:41.2499999...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:01:12.6988636-00:01:18.6647727...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:02.3863636-00:00:12.9545454...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:41.2499999-00:00:56.7613636...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:13.1250000-00:00:26.4204545...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:01:06.8181818-00:01:12.6136363...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:01:24.2045454-00:01:35.1136363...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:56.8465909-00:01:06.8181818...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:26.5056818-00:00:34.6022727...
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: merging...
|Error|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_TML7.bml: Double note at the same time. ignoring...
|Info|Copying files
|Info|Creating F:\workspace\bms\aaa.osz
|Info|Removing F:\workspace\bms\aaa
|Info|------------------------------------------------------------
|Info|Skipped List:
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je10.bms
|Info|O:\BMS Song Pack\Normal\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_TML7.bml
```
</details>

#### 输入包含多个歌曲

```
$ .\BmsToOsu.exe -i "O:\BMS Song Pack\multi" -o bbb --generate-mp3 --no-zip
```

<details>
<summary>
输出
</summary>

```
|Warn|`--no-remove` is appended to the parameter
|Info|Use FFMPEG: C:\bin\ffmpeg.exe
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_b7k.bme
|Info|Processing O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field\_White_Field_7A.bms
|Warn|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_b7k.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field\_White_Field_7H.bms
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_fz5.bms
|Warn|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_fz5.bms: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_fz7.bms
|Info|Processing O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field\_White_Field_7N.bms
|Warn|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_fz7.bms: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je10.bms
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je10.bms: Double play mode; skipping
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je5.bms
|Info|Processing O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field\10-whitefield.bms
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field\10-whitefield.bms: Double play mode; skipping
|Info|Processing O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field\5-whitefield-hd.bms
|Warn|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je5.bms: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_nm7.bme
|Warn|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_nm7.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_nm7a.bme
|Info|Processing O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field\5-whitefield-mx.bms
|Warn|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_nm7a.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_plus_system.bme
|Info|Processing O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field\5-whitefield.bms
|Warn|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_plus_system.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_sla7.bme
|Warn|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_sla7.bme: Bga frame 8a is not founded, ignoring...
|Info|Processing O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_TML7.bml
|Warn|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_TML7.bml: Bga frame 8a is not founded, ignoring...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:01:06.8181818-00:01:12.6136363...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:34.6022727-00:00:41.2499999...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:56.8465909-00:01:06.8181818...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:02.3863636-00:00:12.9545454...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:01:12.6988636-00:01:18.6647727...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:13.1250000-00:00:26.4204545...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:01:18.7499999-00:01:24.2045454...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:01:24.2045454-00:01:35.1136363...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:26.5056818-00:00:34.6022727...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: Generating 00:00:41.2499999-00:00:56.7613636...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:00:01.8750000-00:00:26.4843750...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:00:26.4843750-00:00:43.1250000...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:00:43.1250000-00:00:56.7187500...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:00:56.7187500-00:01:09.5312500...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:01:09.6093750-00:01:22.5000000...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:01:22.5000000-00:01:34.6875000...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:01:34.6875000-00:01:44.1796875...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:01:44.1796875-00:01:52.4218750...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:01:52.5000000-00:02:00.2343750...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: Generating 00:02:00.3515625-00:02:18.7500000...
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆: merging...
|Error|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_TML7.bml: Double note at the same time. ignoring...
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field: merging...
|Info|Copying files
|Info|------------------------------------------------------------
|Info|Skipped List:
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_je10.bms
|Info|O:\BMS Song Pack\multi\[#ねここ14歳] ☆ さくらなみきのかぜ ☆\_nekoko14_TML7.bml
|Info|O:\BMS Song Pack\multi\[[IX] mov ： Optie] White Field\10-whitefield.bms
```
</details>

## Credits

- [vysiondev](https://github.com/vysiondev)

## License

GNU AFFERO GENERAL PUBLIC LICENSE

see [LICENSE](./LICENSE) for more information.

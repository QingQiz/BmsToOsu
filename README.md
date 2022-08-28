# BMS to osu!mania 7k

> Convert bms files to osu beatmap files, including 7k and 7+1k

## Usage

```
  -i, --input        Required. folder containing .bms/.bml/.bme/.bmx
  -o, --output       Required. output folder/filename. e.g. 114514.osz
  --no-sv            (Default: false) weather to include SV
  --no-zip           (Default: false) whether to zip output folder to .osz
  --no-copy          (Default: false) whether to copy sound/image/video files
                     into the output folder
  --no-remove        (Default: false) whether to remove the output folder after
                     zipping it to .osz
  --generate-mp3     (Default: false) generate complete song file from samples
                     of bms
  --ffmpeg           (Default: ) path of ffmpeg
  --max-threads      (Default: 10) max number of ffmpeg threads
  --help             Display this help screen.
```

<details>

<summary>
中文
</summary>

```
  -i, --input       Required. 输入文件夹，可以解析的文件包括 .bms/.bml/.bme/.bmx
  -o, --output      Required. 输出文件夹/文件名
  --no-sv           是否包含 SV（即是否包含变速），默认包含
  --no-zip          是否将输出文件夹压缩成 .osz，默认压缩
  --no-copy         是否将 bms 谱面中的 声音/bga/图片 文件复制到输出文件夹，默认复制
  --no-remove       是否在将输出文件夹压缩成 .osz 后删除输出文件夹，默认删除
  --generate-mp3    是否从 bms 的采样中生成完整 mp3 文件，默认不生成（需要有 ffmpeg）
  --ffmpeg          ffmpeg 可执行文件的路径，默认会在 PATH 中找
  --max-threads     ffmpeg 的最大调用线程数，默认 10（过大会导致在上下文切换中浪费大量的时间）
  --help            展示帮助信息（但是英文）
```
</details>


### Example

1. `.\BmsToOsu.exe -i /path/to/G2R2018 -o /path/to/osu!/Songs/G2R2018 --no-zip --no-remove` will convert G2R2018 to osu beatmaps and put it into the osu! Song folder 
2. `.\BmsToOsu.exe -i /path/to/G2R2018 -o /G2R2018` will generate a .osz file named G2R2018.osz

## Credits

- [vysiondev](https://github.com/vysiondev)

## License

GNU AFFERO GENERAL PUBLIC LICENSE

see [LICENSE](./LICENSE) for more information.

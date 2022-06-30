# BMS to osu!mania 7k

## TODO

- [ ] FIX: Long Note behaves strangely when bpm changes (e.g. [destr0yer][d0]). Suspected to be a bug of osu

## Usage

```shell
cd .\BmsToOsu
dotnet run -- --help
```

### Example

1. `dotnet run -- -i /path/to/G2R2018 -o /path/to/osu!/Songs/G2R2018 --no-zip --no-remove` will convert G2R2018 to osu beatmaps and put it into the osu! Song folder 
2. `dotnet run -- -i /path/to/G2R2018 -o /G2R2018` will generate a .osz file named G2R2018.osz

### Tested on the following beatmaps:

<details>

<summary>
click to expand
</summary>

- normal beatmaps
  - [Calamity Fortune][cf]
  - [All Beatmaps of G2R2018][G2R2018]
- bpm change
  - [Aleph0][a0]
- bga
  - [Credits][credit]
- long note
  - [Destr0yer][d0]

[cf]: http://yaruki0.sakura.ne.jp/event/ondanyugi5/impression.cgi?no=45
[a0]: https://manbow.nothing.sh/event/event.cgi?action=More_def&num=498&event=110
[credit]: https://manbow.nothing.sh/event/event.cgi?action=More_def&num=113&event=104
[d0]: https://manbow.nothing.sh/event/event.cgi?action=More_def&num=353&event=123
[G2R2018]: https://package.bms.ms/G2R2018%20CLIMAX%20-GO%20BACK%202%20YOUR%20ROOTS%202018%20CLIMAX-/

</details>


## Credits

- [vysiondev](https://github.com/vysiondev)

## License

GNU AFFERO GENERAL PUBLIC LICENSE

see [LICENSE](./LICENSE) for more information.

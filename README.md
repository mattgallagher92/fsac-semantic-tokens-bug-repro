# fsautocomplete semantic tokens bug reproduction repo

## Background

When I open Gen.fs in Neovim with fsautocomplete configured as a language server for F# files, my editor soon locks up, and I have to kill the whole terminal.

## Logs

With verbose logging enabled for fsautocomplete (`--verbose` flag) and Neovim (`vim.lsp.log.set_level 'trace'`), I am able to see the following in Neovim's LSP log (accessible at ~/.local/state/nvim/logs/lsp.log) when I open the file.

>...  
>[DEBUG][2026-05-01 15:06:14] /usr/local/share/nvim/runtime/lua/vim/lsp/log.lua:149      "rpc.send"      { id = 3, jsonrpc = "2.0", method = "textDocument/semanticTokens/full", params = { textDocument = { uri = "file:///home/matt/dev/fsac-semantic-tokens-bug-repro/Gen.fs" } } }  
>...  
>[DEBUG][2026-05-01 15:06:17] /usr/local/share/nvim/runtime/lua/vim/lsp/log.lua:149      "rpc.receive"   { id = 3, jsonrpc = "2.0", result = { data = { 0, 7, 5, 0, 0, 0, 6, 3, 25, 0, 2, 5, 7, 0, 0, 2, 5, 13, 1, 0, 0, 14, 2, 6, 0, 0, 8, 2, 6, 0, 0, 18, 13, 10, 0, 0, 17, 2, 6, 0, 0, 3, 3, 2, 0, 2, 4, 26, 12, 0, 0, 27, 13, 8, 0, 1, 8, 1, 8, 0, 1, 13, 1, 8, 0, 1, 12, 13, 8, 0, 1, 12, 2, 21, 0, 0, 3, 3, 25, 0, 0, 3, 8, 12, 0, 1, 12, 2, 21, 0, 0, 3, 3, 25, 0, 0, 3, 4, 12, 0, 0, 6, 3, 25, 0, 0, 3, 9, 12, 0, 0, 13, 1, 21, 0, 0, 2, 1, 8, 0, 0, 3, 2, 21, 0, 0, 3, 3, 2, 0, 0, 4, 2, 21, 0, 0, 3, 13, 10, 0, 2, 8, 2, 21, 0, 0, 3, 3, 25, 0, 0, 3, 6, 12, 0, 2, 8, 2, 12, 0, 0, 4, 13, 10, 0, 0, 13, 1, 8, 0, 0, 1, 6, 8, 0, 1, 8, 6, 8, 0, 1, 8, 2, 21, 0, 0, 3, 3, 25, 0, 0, 3, 1, 13, 0, 0, 0, 4294967293, 13, 0, 0, 1, 7, 2, 0, 0, 7, 4, 13, 0, 0, 0, 15, 13, 0, 0, 4, 4294967285, 13, 0, 1, 8, 2, 21, 0, 0, 3, 3, 25, 0, 0, 3, 7, 12, 0, 0, 13, 1, 8, 0, 0, 5, 1, 8, 0, 0, 1, 6, 9, 0, 0, 7, 1, 21, 0, 1, 8, 2, 21, 0, 0, 3, 3, 25, 0, 0, 3, 4, 12, 0, 0, 5, 13, 10, 0, 2, 4, 3, 25, 0, 0, 3, 14, 12, 0, 0, 16, 1, 8, 0, 0, 3, 2, 12, 0 } } }  

Obviously I've omitted lots, but the "rpc.receive" message above is the last thing logged before the editor locks up.

## Reproducing

Use the configuration in the [./nvimconfig/](./nvimconfig/) subdirectory (e.g. place its contents in ~/.config/nvim/) and open Gen.fs in Neovim (run `nvim Gen.fs`) if you want to see this for yourself. You'll need fsautocomplete installed, which can be achieved with `dotnet tool install --global fsautocomplete@0.83.0`).

Of course, you should be able to see similar LSP logs with different editors.

## The presumed problem

As defined in the [LSP specification for the SemanticTokens type](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#semanticTokens), the value of the `data` field should be an array of [uintegers](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#uinteger), that is "an unsigned integer number in the range of 0 to 2^31 - 1". 4294967293 and 4294967285 are outside that range, so including them in the response is a violation of LSP.

In addition, the [specification for the semanticTokens capability](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_semanticTokens) describes the meaning of the array:

>A specific token i in the file consists of the following array indices:
>
>- at index 5*i - deltaLine: token line number, relative to the start of the previous token
>- at index 5*i+1 - deltaStart: token start character, relative to the start of the previous token (relative to 0 or the previous token’s start if they are on the same line)
>- at index 5*i+2 - length: the length of the token.
>- at index 5*i+3 - tokenType: will be looked up in SemanticTokensLegend.tokenTypes. We currently ask that tokenType < 65536.
>- at index 5*i+4 - tokenModifiers: each set bit will be looked up in SemanticTokensLegend.tokenModifiers

It seems that fsautocomplete is stating that some tokens have length around 4 billion, which is clearly wrong.

I suspect that Neovim is freezing either because of the protocol violation or because it's trying to do a computation that takes a long time when the length is 4 billion.

[FsAutoComplete#1407](https://github.com/ionide/FsAutoComplete/issues/1407) seems to report the same issue. [neovim#36257](https://github.com/neovim/neovim/issues/36257) offers a workaround.

return {
  filetypes = { 'fsharp' },
  root_markers = { '*.slnx', '*.sln', '*.fsproj', '.git' },
  cmd = { 'fsautocomplete', '--verbose' },
  -- https://github.com/ionide/FsAutoComplete/blob/main/docs/communication-protocol.md#initialization-options
  init_options = {
    AutomaticWorkspaceInit = true,
  },
}

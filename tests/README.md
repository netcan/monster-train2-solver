# Tests and native evidence

- [fixtures](fixtures/README.md): native regression archives grouped by feature,
  acceptance notes, inventories and source checkpoints.
- [catalogs](catalogs/README.md): complete native card and boss definition catalogs.
- [packaging](packaging/): installer verification scripts.

`scripts/Check-FixtureCatalog.ps1` validates all curated archive locations and
hashes without replaying battles. `scripts/Check-Models.ps1 -Fixture <name>`
selects an archive by its stable name, independently of its subdirectory.

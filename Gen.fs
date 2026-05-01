module Tests.Gen

open FsCheck

type SetOfAtLeast2<'a when 'a: comparison> = SetOfAtLeast2 of 'a Set

let arbitrarySetOfAtLeast2From possibilities =
    let g =
        (fun s ->
            possibilities
            |> Gen.shuffle
            |> Gen.map (Seq.truncate (2 + s) >> Set >> SetOfAtLeast2)
        )
        |> Gen.sized

    let sh (SetOfAtLeast2 failed) =
        failed
        |> Arb.Default.Set().Shrinker
        |> Seq.filter (fun s -> s.Count > 2)
        |> Seq.map SetOfAtLeast2

    Arb.fromGenShrink (g, sh)

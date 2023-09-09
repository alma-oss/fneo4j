namespace Alma.Neo4j

//
// DTOs - must be public
//
module public Dto =
    open Alma.Neo4j

    // Common

    type Name () =
        let mutable nameValue = ""

        member __.Name
            with get() = nameValue
            and set (value) = nameValue <- value

    type NameAndLabel () =
        let mutable nameValue = ""
        let mutable labelValue = ""

        member __.Name
            with get() = nameValue
            and set (value) = nameValue <- value

        member __.Label
            with get() = labelValue
            and set (value) = labelValue <- value

    let withNameAndLabel name label =
        NameAndLabel (
            Name = (name |> Name.value),
            Label = (label |> Label.value)
        )

    let withName name =
        Name (
            Name = (name |> Name.value)
        )

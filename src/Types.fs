namespace Lmc.Neo4j

[<AutoOpen>]
module private Neo4jStringModule =
    type Neo4jString = private Neo4jString of string

    [<RequireQualifiedAccess>]
    module Neo4jString =
        let create = Neo4jString
        let value (Neo4jString string) = string |> String.replaceAll [ "~"; "^"; "{"; "}" ] ""
        let format = create >> value

[<AutoOpen>]
module PropertyNameModule =
    type PropertyName = private PropertyName of Neo4jString

    [<RequireQualifiedAccess>]
    module PropertyName =
        let create = Neo4jString.create >> PropertyName
        let value (PropertyName property) = property |> Neo4jString.value |> String.ucFirst

[<AutoOpen>]
module NameModule =
    type Format<'Value> = 'Value -> string
    type Name<'Value> = Name of 'Value * Format<'Value>

    type FormattedName = private FormattedName of Neo4jString

    [<RequireQualifiedAccess>]
    module FormattedName =
        let value (FormattedName name) = name |> Neo4jString.value

    [<RequireQualifiedAccess>]
    module Name =
        let Property = PropertyName.create "Name"

        let format (Name (value, format)) = format value |> Neo4jString.create |> FormattedName

        let map mapValue mapFormat (Name (value, format)) =
            Name (value |> mapValue, mapFormat >> format)

        let value name = name |> format |> FormattedName.value

        let ofString (name: string) =
            Name (name, id)

[<AutoOpen>]
module LabelModule =
    type Format<'Value> = 'Value -> string
    type Label<'Value> = Label of 'Value * Format<'Value>

    type FormattedLabel = private FormattedLabel of Neo4jString

    [<RequireQualifiedAccess>]
    module FormattedLabel =
        let value (FormattedLabel label) = label |> Neo4jString.value

    [<RequireQualifiedAccess>]
    module Label =
        let format (Label (value, format)) = format value |> Neo4jString.create |> FormattedLabel
        let value label = label |> format |> FormattedLabel.value

[<AutoOpen>]
module NodeTypeModule =
    type NodeType = private NodeType of string list

    [<RequireQualifiedAccess>]
    module NodeType =
        let create nodeType = NodeType [ nodeType ]
        let createMultiple = NodeType

        let ofString = create

        let add (NodeType a) (NodeType b) = NodeType (a @ b)

        let private normalize (nodeType: string) = nodeType.ToUpper().Replace("*", "")

        let value (NodeType nodeTypes) =
            nodeTypes
            |> List.map normalize
            |> List.distinct
            |> String.concat ":"

[<AutoOpen>]
module NodeIdModule =
    type NodeId = private NodeId of string

    [<RequireQualifiedAccess>]
    module NodeId =
        let private ofValues (values: string list) =
            values
            |> List.map ((String.replaceAll [ ":"; ";"; "."; "@"; "#"; "&" ] "_") >> String.toLower >> String.ucFirst)
            |> String.concat ""
            |> NodeId

        let ofString = NodeId

        let create nodeType name =
            ofValues [
                yield name |> Name.value
                yield! nodeType |> NodeType.value |> String.split "_"
            ]

        let value (NodeId nodeId) = nodeId.Replace("-", "").Replace("*", "Any")

[<AutoOpen>]
module LinkTypeModule =
    type LinkType = private LinkType of string

    [<RequireQualifiedAccess>]
    module LinkType =
        let create = LinkType
        let value (LinkType linkType) = linkType.ToUpper()

[<AutoOpen>]
module PlaceholderModule =
    type Placeholder = private Placeholder of Neo4jString

    [<RequireQualifiedAccess>]
    module Placeholder =
        let withId nodeId placeholder = sprintf "%s%s" (nodeId |> NodeId.value) placeholder |> Neo4jString.create |> Placeholder
        let value (Placeholder placeholder) = placeholder |> Neo4jString.value

type Attributes =
    | Attributes of Map<PropertyName, string>

    /// Merge attributes (new attributes will override current)
    static member (+) ((Attributes currentAttributes), (Attributes newAttributes)) =
        Attributes (newAttributes |> Map.merge currentAttributes)

[<RequireQualifiedAccess>]
module Attributes =
    let empty: Attributes = Attributes Map.empty

    let from values =
        Attributes (values |> Map.ofList)

    let format (Attributes attributes) =
        match attributes |> Map.toList with
        | [] -> ""
        | attributes ->
            attributes
            |> List.fold (fun acc (key, value) ->
                sprintf "%s: '%s'" (key |> PropertyName.value) (value |> Neo4jString.format) :: acc
            ) []
            |> List.rev
            |> String.concat ", "
            |> sprintf "{ %s }"

type GraphQueryNodeForLink = {
    Id: NodeId
    Type: NodeType
    Name: FormattedName
}

type GraphQueryNode<'Value> = {
    Type: NodeType
    Name: Name<'Value>
    Dto: obj
}

[<RequireQualifiedAccess>]
module GraphQueryNode =
    let id node =
        NodeId.create node.Type node.Name

    let forLink (node: GraphQueryNode<_>) =
        {
            Id = node |> id
            Type = node.Type
            Name = node.Name |> Name.format
        }

    let map mapValue mapFormat node =
        {
            Type = node.Type
            Name = node.Name |> Name.map mapValue mapFormat
            Dto = node.Dto
        }

type Link = {
    From: GraphQueryNodeForLink
    Type: LinkType
    Attributes: Attributes
    To: GraphQueryNodeForLink
}

[<RequireQualifiedAccess>]
module Link =
    let toCypher link =
        sprintf "(%s)-[:%s%s]->(%s)"
            (link.From.Id |> NodeId.value)
            (link.Type |> LinkType.value)
            (link.Attributes |> Attributes.format)
            (link.To.Id |> NodeId.value)

    module Operators =
        type PartialLink<'Value> = private {
            From: GraphQueryNode<'Value>
            Type: string
            Attributes: Attributes
        }

        /// Start link
        let (-|) fromNode linkType =
            {
                From = fromNode
                Type = linkType
                Attributes = Attributes.empty
            }

        /// Add attributes to link
        let (&>) (partialLink: PartialLink<_>) attributes =
            { partialLink with Attributes = partialLink.Attributes + Attributes.from attributes }

        /// Finish link
        let (|->) partialLink toNode =
            {
                From = partialLink.From |> GraphQueryNode.forLink
                Type = LinkType.create partialLink.Type
                Attributes = partialLink.Attributes
                To = toNode |> GraphQueryNode.forLink
            }

using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// Imports .yarn files as TextAssets.
///
/// Unity's built-in TextAsset importer only claims a fixed extension list
/// (.txt, .json, .xml, .csv, .md, .yaml, .bytes, .html, .htm, .fnt). ".yarn" is
/// not on it, so without this the files import as generic DefaultAssets and
/// cannot be dragged into a TextAsset field such as DialogueManager's.
///
/// NOTE: if the Yarn Spinner package is ever installed it registers its own
/// importer for this extension, and Unity refuses to have two importers on one
/// extension ("Multiple ScriptedImporters are targeting the extension 'yarn'").
/// Delete this file at that point — Yarn Spinner's importer supersedes it.
/// </summary>
[ScriptedImporter(version: 1, ext: "yarn")]
public class YarnTextImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var text = new TextAsset(File.ReadAllText(ctx.assetPath));

        // "main obj" is just the identifier within the asset; the name shown in
        // the project window comes from the file itself.
        ctx.AddObjectToAsset("main obj", text);
        ctx.SetMainObject(text);
    }
}

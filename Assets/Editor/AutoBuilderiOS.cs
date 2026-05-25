using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class AutoBuilderIOS
{
    // Ce code s'exécute automatiquement juste après la compilation pour iOS
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            // Trouver le fichier Info.plist généré par Unity
            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            // Ouvrir la racine du fichier
            PlistElementDict rootDict = plist.root;

            // Ajouter la clé de transparence du suivi (ATT)
            string attMessage = "Vos données seront utilisées pour vous proposer des publicités personnalisées et pertinentes pour soutenir le jeu.";
            rootDict.SetString("NSUserTrackingUsageDescription", attMessage);

            // (Optionnel) Si vous voulez être sûr d'ajouter aussi le GADApplicationIdentifier automatiquement :
            // rootDict.SetString("GADApplicationIdentifier", "ca-app-pub-VOTRE_ID_COMPLET_ICI");

            // Sauvegarder les modifications
            plist.WriteToFile(plistPath);
        }
    }
}
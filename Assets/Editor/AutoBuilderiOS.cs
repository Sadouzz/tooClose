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
            string plistPath = path + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            // Ouvrir la racine du fichier
            PlistElementDict rootDict = plist.root;

            // Ajouter la clé de transparence du suivi (ATT)
            string attMessage = "Vos données seront utilisées pour vous proposer des publicités personnalisées et pertinentes pour soutenir le jeu.";
            rootDict.SetString("NSUserTrackingUsageDescription", attMessage);

            // (Optionnel) Si vous voulez être sûr d'ajouter aussi le GADApplicationIdentifier automatiquement, décommentez la ligne en dessous et mettez votre ID :
            // rootDict.SetString("GADApplicationIdentifier", "ca-app-pub-VOTRE_ID_COMPLET_ICI");

            // Sauvegarder et fermer le fichier
            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
}
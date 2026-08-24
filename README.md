# Clacp

Ce depot contient deux applications Windows independantes qui simulent la frappe au clavier. Developpees par Lylian Fredon « Daryu ».

- **Clacp** (`src/Clacp`) : version complete, avec coffre de mots de passe optionnel.
- **IPTEK** (`src/Iptek`) : version allegee, uniquement la frappe depuis le presse-papiers (pas de coffre de mots de passe).

Les deux partagent le meme moteur de frappe (compatible RDP), le meme systeme de theme clair/sombre, et le meme mode de fonctionnement (raccourci global configurable + delai avant frappe + notification).

## Fonctionnalites (Clacp)

- **Raccourci presse-papiers** (Ctrl+Alt+L par defaut, configurable) : tape le contenu du presse-papiers dans la fenetre active, avec un delai configurable pour laisser le temps a la fenetre cible de reprendre le focus. Fonctionne des l'installation, sans configuration.
- **Notification** discrete en bas a droite une fois la frappe terminee.
- **Coffre de mots de passe** (desactive par defaut, activable dans Parametres) : liste d'entrees (titre, identifiant, mot de passe, URL, notes), generateur de mot de passe, raccourci de recherche rapide (Ctrl+Alt+P par defaut) pour taper une entree.
  - Chiffre en permanence : soit lie a votre session Windows (DPAPI, aucun mot de passe a saisir), soit protege par un mot de passe maitre (AES-256-GCM, cle derivee via PBKDF2) si vous activez cette option.
- **Verrouillage automatique** du coffre protege apres N minutes d'inactivite.
- **Compatible RDP** : la frappe utilise des scan codes plutot que des paquets Unicode synthetiques, pour fonctionner dans les sessions Bureau a distance.
- **Theme clair ou sombre**, au choix dans Parametres.
- **Demarrage automatique avec Windows** (optionnel).
- Icone dans la barre des taches ; l'application reste active en arriere-plan pour que les raccourcis fonctionnent (fermer la fenetre la minimise dans la barre des taches, "Quitter" depuis le menu de l'icone pour arreter vraiment).
- Une seule instance a la fois (relancer l'app ramene la fenetre existante au premier plan).

## Fonctionnalites (IPTEK)

Meme moteur que Clacp (raccourci presse-papiers, delai configurable, notification, theme, demarrage avec Windows, instance unique), sans la brique coffre de mots de passe : pas d'onglet Coffre, pas de raccourci de recherche, pas de stockage chiffre.

## Prerequis (pour compiler)

- Windows
- [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)

## Lancer le projet

```powershell
dotnet run --project src/Clacp/Clacp.csproj
# ou
dotnet run --project src/Iptek/Iptek.csproj
```

## Build

```powershell
dotnet build
```

## Generer un executable autonome (sans SDK requis sur la machine cible)

```powershell
dotnet publish src/Clacp/Clacp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
dotnet publish src/Iptek/Iptek.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish-iptek
```

Produit un executable unique (`publish/Clacp.exe` ou `publish-iptek/Iptek.exe`), copiable et executable sur n'importe quel Windows 10/11 x64 sans installer .NET.

## Generer l'installateur (avec licence et desinstallation)

Necessite [Inno Setup 6](https://jrsoftware.org/isinfo.php). Publier d'abord l'executable autonome correspondant (etape precedente), puis :

```powershell
& "$env:LocalAppData\Programs\Inno Setup 6\ISCC.exe" installer\Clacp.iss
& "$env:LocalAppData\Programs\Inno Setup 6\ISCC.exe" installer\Iptek.iss
```

Produit `installer-output\Clacp-Setup.exe` et/ou `installer-output\Iptek-Setup.exe` : installation par utilisateur (pas de droits admin requis), ecran de licence obligatoire (`LICENSE.txt` / `LICENSE-IPTEK.txt`), raccourcis Menu Demarrer/Bureau optionnels, et entree de desinstallation standard dans "Applications installees" de Windows.

Ces deux fichiers `-Setup.exe` ne sont pas suivis dans le depot (build local uniquement) : a regenerer a chaque fois avec les commandes ci-dessus.

## Licence

Voir [LICENSE.txt](LICENSE.txt) (Clacp) et [LICENSE-IPTEK.txt](LICENSE-IPTEK.txt) (IPTEK). Logiciels proprietaires : reutilisation, modification et revente interdites sans l'accord ecrit de l'auteur.

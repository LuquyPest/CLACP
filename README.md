# Clacp

Gestionnaire de mots de passe Windows avec frappe automatique. Les entrees sont stockees dans un coffre chiffre (AES-256-GCM, cle derivee du mot de passe maitre via PBKDF2), et un raccourci clavier global permet de rechercher une entree et de la "taper" directement dans la fenetre active, comme si vous tapiez au clavier.

## Fonctionnalites

- Coffre chiffre localement (`%AppData%\Clacp\vault.dat`), protege par un mot de passe maitre.
- Raccourci global **Ctrl+Alt+P** : ouvre une recherche rapide, selectionnez une entree pour la taper dans la fenetre active (utilisateur puis Tab puis mot de passe, ou mot de passe seul selon l'entree).
- Generateur de mot de passe.
- Icone dans la barre des taches (l'app reste active en arriere-plan pour que le raccourci fonctionne).

## Prerequis

- Windows
- [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)

## Lancer le projet

```powershell
dotnet run --project src/Clacp/Clacp.csproj
```

## Build

```powershell
dotnet build
```

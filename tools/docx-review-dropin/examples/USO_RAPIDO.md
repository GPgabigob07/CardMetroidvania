# Uso rápido

Copie esta pasta para perto do documento e rode:

```powershell
.\run_review.ps1 ".\TIC.docx" -Zip
```

Se o Word abrir alertas ou a renderização falhar:

```powershell
.\run_review.ps1 ".\TIC.docx" -NoRender -Zip
```

Para só encontrar problemas, sem gerar cópia editada:

```powershell
.\run_review.ps1 ".\TIC.docx" -AuditOnly -Zip
```

Antes de habilitar substituições, edite:

```text
safe_replacements.json
```

Antes de alterar a lista de estrangeirismos, edite:

```text
foreign_terms.txt
```

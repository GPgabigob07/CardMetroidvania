# DOCX Review Drop-in

Kit pequeno para revisar trabalhos acadêmicos em `.docx` com foco em ABNT, preservando o original e registrando tudo que for detectado ou alterado.

Ele foi pensado para revisões como as do TIC: gramática, estrutura, citações, referências, ludografia, imagens, layout e pequenas correções mecânicas.

## Requisitos

- Python 3.10 ou superior.
- Para renderizar PDF: Microsoft Word no Windows ou LibreOffice no PATH.
- Para gerar PNG das páginas: `pdftoppm` opcional.
- Não exige `pip install` para auditoria/edição segura.

## Uso rápido

No PowerShell, dentro da pasta deste kit:

```powershell
.\run_review.ps1 "C:\caminho\documento.docx" -Zip
```

Ou diretamente com Python:

```powershell
python scripts\review_docx.py "C:\caminho\documento.docx" --zip
```

Modo só auditoria, sem criar cópia editada:

```powershell
python scripts\review_docx.py "C:\caminho\documento.docx" --audit-only --zip
```

Sem renderizar PDF:

```powershell
python scripts\review_docx.py "C:\caminho\documento.docx" --no-render --zip
```

## O que o fluxo gera

Uma pasta ao lado do `.docx`, com nome parecido com:

```text
REVISAO_NOME_DO_DOCUMENTO_20260814-1530
```

Dentro dela:

- snapshot do original;
- cópia editada, quando não estiver em modo `--audit-only`;
- `audit-original/`;
- `audit-editado/`;
- `alteracoes/`;
- `render-editado/`;
- `LEIA-ME-RESULTADO.md`;
- ZIP final, se `--zip` for usado.

## O que é aplicado automaticamente

Por padrão, o script pode:

- colocar em itálico os termos listados em `foreign_terms.txt`;
- normalizar quebras internas em parágrafos de referência/URL;
- aplicar substituições explícitas habilitadas em `safe_replacements.json`.

O original nunca é alterado.

## O que não é aplicado automaticamente

- Reescrita de parágrafo.
- Troca de citação por outra fonte.
- Remoção de referência.
- Mudança estrutural.
- Alteração de imagens.
- Remoção ampla de negrito.

Esses pontos aparecem nos relatórios para decisão humana.

## Configurar estrangeirismos

Edite `foreign_terms.txt`, um termo por linha.

Exemplo:

```text
briefing
pipeline
concept art
model sheet
```

## Configurar substituições

Edite `safe_replacements.json` e mude `enabled` para `true` apenas quando a troca for segura.

Exemplo:

```json
{
  "enabled": true,
  "find": "RELATORIO",
  "replace": "RELATÓRIO",
  "case_sensitive": true,
  "note": "Correção mecânica de título."
}
```

## Scripts individuais

Auditoria:

```powershell
python scripts\audit_docx.py "documento.docx" --out "saida\audit" --terms foreign_terms.txt
```

Edição segura:

```powershell
python scripts\apply_safe_formatting.py "documento.docx" --out "documento_EDITADO.docx" --terms foreign_terms.txt --replacements safe_replacements.json --log-dir "saida\alteracoes"
```

Renderização:

```powershell
python scripts\render_docx.py "documento_EDITADO.docx" --out "saida\render" --png
```

## Limitações importantes

- A detecção de citações é heurística; ela aponta suspeitas, não sentencia.
- O script não busca fontes na web. Ele apenas encontra citações órfãs e referências possivelmente não citadas.
- A renderização depende de Word ou LibreOffice instalados.
- DOCX com campos complexos, macros ou formatação institucional muito específica sempre deve ser conferido visualmente no Word.

## Fluxo recomendado para entrega

1. Rodar `review_docx.py`.
2. Abrir `citacoes-e-referencias.md`.
3. Resolver citações órfãs manualmente.
4. Abrir a cópia editada no Word.
5. Atualizar sumário/listas, se necessário.
6. Conferir PDF renderizado.
7. Enviar cópia editada + relatórios.

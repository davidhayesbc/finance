# Privestio UI Review With GPT-5.4

Use GPT-5.4 to review the current Privestio UI using the attached screenshots as the primary visual evidence.

Add these references:

- `#docs/UI-REVIEW-GPT54.md`
- `#src/Privestio.Web/Pages/Home.razor`
- `#src/Privestio.Web/Pages/Login.razor`
- `#src/Privestio.Web/Pages/Register.razor`
- `#src/Privestio.Web/Pages/Accounts.razor`
- `#src/Privestio.Web/Pages/Import.razor`

Instructions:

1. Use the screenshots from `artifacts/ui-review/` as the visual source of truth. Do not speculate about rendered UI behavior that is not visible.
2. Review the UI against these constraints:
   - preserve the existing Fluent UI visual language and the current product tone
   - check that the first viewport reads as one composition instead of several competing blocks
   - look for weak hierarchy, clutter, weak CTA emphasis, awkward spacing, inconsistent typography, and unclear empty or loading states
   - assess both desktop and mobile screenshots
   - prefer changes that are realistic in the current Blazor application
3. Apply the GPT-5.4 frontend guidance pragmatically:
   - stronger visual anchor
   - fewer competing elements above the fold
   - clearer narrative from section to section
   - deliberate use of color, contrast, and spacing
4. Do not default to recommending a total redesign. Favor high-leverage incremental improvements.

Output format:

1. Findings ordered by severity.
2. For each finding include:
   - route or screenshot name
   - what is wrong
   - why it matters
   - the recommended change
3. Include a short section for what is already working.
4. End with the top 5 changes that would most improve the product.

Use a direct, code-review style tone.

type JsonLdNode = Record<string, unknown>;

// Structured data for search engines (docs/seo.md). React renders a script's string child as
// raw text; "<" is escaped as < (still valid JSON) so a title can never close the element.
export function JsonLd({ data }: { data: JsonLdNode | JsonLdNode[] }) {
  const document = Array.isArray(data)
    ? { "@context": "https://schema.org", "@graph": data }
    : { "@context": "https://schema.org", ...data };
  return (
    <script type="application/ld+json">{JSON.stringify(document).replace(/</g, "\\u003c")}</script>
  );
}

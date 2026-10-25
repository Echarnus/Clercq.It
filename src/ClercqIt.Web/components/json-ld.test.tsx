import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { JsonLd } from "./json-ld";

describe("JsonLd", () => {
  it("renders schema.org JSON that cannot close its script element", () => {
    const { container } = render(<JsonLd data={{ "@type": "Thing", name: "</script><b>x" }} />);
    const script = container.querySelector('script[type="application/ld+json"]');
    expect(script?.textContent).not.toContain("</script");
    expect(JSON.parse(script?.textContent ?? "")).toEqual({
      "@context": "https://schema.org",
      "@type": "Thing",
      name: "</script><b>x",
    });
  });

  it("wraps several nodes in a graph", () => {
    const { container } = render(<JsonLd data={[{ "@type": "Person" }, { "@type": "WebSite" }]} />);
    const json = JSON.parse(container.querySelector("script")?.textContent ?? "") as {
      "@graph": unknown[];
    };
    expect(json["@graph"]).toHaveLength(2);
  });
});

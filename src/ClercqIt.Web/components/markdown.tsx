import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { cn } from "@/lib/utils";

/**
 * Renders a Markdown body (projects, blogs, certifications; see docs/content.md) with GitHub
 * Flavored Markdown. Raw HTML in the source is shown as text, never rendered, so content
 * cannot inject markup or scripts. External links open in a new tab.
 */
export function Markdown({ children, className }: { children: string; className?: string }) {
  return (
    <div className={cn("prose max-w-none prose-slate dark:prose-invert", className)}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          a: ({ href, children, ...props }) => {
            const external = href?.startsWith("http");
            return (
              <a
                href={href}
                {...props}
                {...(external ? { target: "_blank", rel: "noopener noreferrer" } : {})}
              >
                {children}
              </a>
            );
          },
        }}
      >
        {children}
      </ReactMarkdown>
    </div>
  );
}

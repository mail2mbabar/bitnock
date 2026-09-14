import { ArticleEditor } from "@/components/admin/ArticleEditor";

export default function NewArticlePage() {
  return (
    <div>
      <h1 className="mb-6 font-serif text-3xl">Create article</h1>
      <ArticleEditor />
    </div>
  );
}

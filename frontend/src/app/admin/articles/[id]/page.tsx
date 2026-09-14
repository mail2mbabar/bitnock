import { ArticleEditor } from "@/components/admin/ArticleEditor";

export default async function EditArticlePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return (
    <div>
      <h1 className="mb-6 font-serif text-3xl">Edit article</h1>
      <ArticleEditor articleId={id} />
    </div>
  );
}

import { BoardApp } from "@/features/board/BoardApp";
import { getBoard } from "@/lib/api-client";

export const dynamic = "force-dynamic";

export default async function HomePage() {
    const apiInternalUrl =
        process.env.API_INTERNAL_URL ?? "http://localhost:5147";
    const board = await getBoard(apiInternalUrl);

    return <BoardApp initialBoard={board} />;
}

import React, { useState } from "react";
import { useQuery, useMutation } from "@apollo/client/react";
import { FiMessageSquare, FiSend, FiCornerDownRight, FiEdit2, FiTrash2 } from "react-icons/fi";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";

import { GET_TASK_COMMENTS } from "../../../graphql/queries/task/taskComment/taskCommentQueries";
import { 
    CREATE_TASK_COMMENT,
    UPDATE_TASK_COMMENT, 
    DELETE_TASK_COMMENT  
} from "../../../graphql/mutations/taskmut/taskComment/taskCommentMutations"; 

type Membership = {
    userId: string;
    user: { name: string; };
};

type Props = {
    taskId: string;
    memberships: Membership[];
    currentUserId?: string; 
};

type TaskComment = {
    id: string;
    authorId: string;
    parentCommentId: string | null;
    body: string;
    createdAt: string;
    isEdited: boolean;
    isDeleted: boolean;
};

type TaskCommentsResponse = {
    taskComments: TaskComment[];
};

export const TaskCommentsSection: React.FC<Props> = ({ taskId, memberships, currentUserId }) => {
    const [newComment, setNewComment] = useState("");
    const [replyingTo, setReplyingTo] = useState<string | null>(null);
    const [error, setError] = useState("");

    const [editingId, setEditingId] = useState<string | null>(null);
    const [editBody, setEditBody] = useState("");

    const { data, loading, refetch } = useQuery<TaskCommentsResponse>(GET_TASK_COMMENTS, {
        variables: { taskId },
        fetchPolicy: "cache-and-network",
    });

    const [createComment, { loading: creating }] = useMutation(CREATE_TASK_COMMENT, {
        onCompleted: () => {
            setNewComment("");
            setReplyingTo(null);
            setError("");
            refetch();
        },
        onError: (err) => setError(getFriendlyErrorMessage(err) ?? "Не вдалося відправити коментар")
    });

    const [updateComment, { loading: updating }] = useMutation(UPDATE_TASK_COMMENT, {
        onCompleted: () => {
            setEditingId(null);
            setError("");
            refetch();
        },
        onError: (err) => setError(getFriendlyErrorMessage(err) ?? "Не вдалося оновити коментар")
    });

    const [deleteComment] = useMutation(DELETE_TASK_COMMENT, {
        onCompleted: () => {
            setError("");
            refetch();
        },
        onError: (err) => setError(getFriendlyErrorMessage(err) ?? "Не вдалося видалити коментар")
    });

    const comments: TaskComment[] = data?.taskComments ?? [];

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newComment.trim()) return;

        await createComment({
            variables: { input: { taskId, parentCommentId: replyingTo, body: newComment.trim() } }
        });
    };

    const handleSaveEdit = async (commentId: string) => {
        if (!editBody.trim()) return;
        await updateComment({
            variables: { input: { commentId, body: editBody.trim() } }
        });
    };

    const handleDelete = async (commentId: string) => {
        if (window.confirm("Ви впевнені, що хочете видалити цей коментар?")) {
            await deleteComment({
                variables: { input: { commentId } }
            });
        }
    };

    const getAuthorName = (authorId: string) => {
        const member = memberships.find(m => m.userId === authorId);
        return member?.user.name ?? "Невідомий користувач";
    };

    function formatTime(dateString: string) {
        return new Date(dateString).toLocaleString("uk-UA", {
            day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit"
        });
    }

    return (
        <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm mt-6">
            <div className="flex items-center gap-2 mb-6">
                <FiMessageSquare className="text-blue-600" size={20} />
                <h3 className="text-lg font-bold text-[#1f2937]">Обговорення</h3>
                <span className="ml-2 bg-gray-100 text-gray-600 text-xs py-0.5 px-2 rounded-full font-medium">
                    {comments.length}
                </span>
            </div>

            {error && (
                <div className="mb-4 text-xs text-red-600 bg-red-50 p-3 rounded-xl border border-red-100">
                    {error}
                </div>
            )}

            <div className="space-y-4 mb-6">
                {loading ? (
                    <div className="text-sm text-gray-500 text-center py-4">Завантаження коментарів...</div>
                ) : comments.length === 0 ? (
                    <div className="text-sm text-gray-400 italic text-center py-8 bg-gray-50 rounded-xl border border-gray-100 border-dashed">
                        Коментарів ще немає. Будь першим!
                    </div>
                ) : (
                    comments.map((comment) => (
                        <div 
                            key={comment.id} 
                            className={`p-4 rounded-xl border transition ${
                                comment.parentCommentId 
                                    ? "ml-8 bg-gray-50 border-gray-100" 
                                    : "bg-white border-gray-200"
                            }`}
                        >
                            <div className="flex justify-between items-start mb-2">
                                <div className="flex items-center gap-2">
                                    <span className="font-semibold text-sm text-[#1f2937]">
                                        {getAuthorName(comment.authorId)}
                                    </span>
                                    <span className="text-[11px] text-gray-400">
                                        {formatTime(comment.createdAt)}
                                    </span>
                                    {comment.isEdited && (
                                        <span className="text-[10px] text-gray-400 italic">
                                            (редаговано)
                                        </span>
                                    )}
                                </div>
                            </div>
                            
                            {/* БЛОК ТЕКСТУ АБО ФОРМИ РЕДАГУВАННЯ */}
                            <div className="text-sm text-[#374151] whitespace-pre-wrap mt-1">
                                {comment.isDeleted ? (
                                    <span className="italic text-gray-400">Коментар видалено</span>
                                ) : editingId === comment.id ? (
                                    <div className="mt-2">
                                        <textarea
                                            value={editBody}
                                            onChange={(e) => setEditBody(e.target.value)}
                                            className="w-full rounded-xl border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
                                            rows={2}
                                        />
                                        <div className="flex gap-2 mt-2">
                                            <button 
                                                onClick={() => handleSaveEdit(comment.id)} 
                                                disabled={updating || !editBody.trim()}
                                                className="text-xs bg-blue-600 text-white px-3 py-1.5 rounded-lg hover:bg-blue-700 disabled:opacity-50"
                                            >
                                                {updating ? "Збереження..." : "Зберегти"}
                                            </button>
                                            <button 
                                                onClick={() => setEditingId(null)} 
                                                className="text-xs bg-gray-100 text-gray-600 px-3 py-1.5 rounded-lg hover:bg-gray-200"
                                            >
                                                Скасувати
                                            </button>
                                        </div>
                                    </div>
                                ) : (
                                    // Звичайний текст
                                    comment.body
                                )}
                            </div>

                            {/* КНОПКИ ДІЙ */}
                            {!comment.isDeleted && editingId !== comment.id && (
                                <div className="flex items-center gap-4 mt-3">
                                    {!comment.parentCommentId && (
                                        <button 
                                            onClick={() => setReplyingTo(comment.id)}
                                            className="flex items-center gap-1 text-xs font-medium text-gray-400 hover:text-blue-600 transition"
                                        >
                                            <FiCornerDownRight size={12} /> Відповісти
                                        </button>
                                    )}
                                    
                                    {(!currentUserId || currentUserId === comment.authorId) && (
                                        <>
                                            <button 
                                                onClick={() => {
                                                    setEditingId(comment.id);
                                                    setEditBody(comment.body);
                                                }}
                                                className="flex items-center gap-1 text-xs font-medium text-gray-400 hover:text-green-600 transition"
                                            >
                                                <FiEdit2 size={12} /> Редагувати
                                            </button>
                                            <button 
                                                onClick={() => handleDelete(comment.id)}
                                                className="flex items-center gap-1 text-xs font-medium text-gray-400 hover:text-red-600 transition"
                                            >
                                                <FiTrash2 size={12} /> Видалити
                                            </button>
                                        </>
                                    )}
                                </div>
                            )}
                        </div>
                    ))
                )}
            </div>

            <form onSubmit={handleSubmit} className="relative">
                {replyingTo && (
                    <div className="flex justify-between items-center bg-blue-50 text-blue-700 px-3 py-1.5 rounded-t-lg text-xs font-medium border border-blue-100 border-b-0">
                        <span>Відповідь на коментар...</span>
                        <button type="button" onClick={() => setReplyingTo(null)} className="hover:text-blue-900">
                            Скасувати
                        </button>
                    </div>
                )}
                <div className="flex gap-3">
                    <textarea
                        value={newComment}
                        onChange={(e) => setNewComment(e.target.value)}
                        placeholder="Написати коментар..."
                        className={`w-full rounded-xl border border-gray-300 bg-gray-50/50 px-4 py-3 text-sm outline-none transition focus:border-blue-500 focus:bg-white resize-none h-[52px] ${replyingTo ? 'rounded-tl-none' : ''}`}
                        rows={1}
                        onKeyDown={(e) => {
                            if (e.key === 'Enter' && !e.shiftKey) {
                                e.preventDefault();
                                handleSubmit(e);
                            }
                        }}
                    />
                    <button 
                        type="submit" 
                        disabled={!newComment.trim() || creating}
                        className="flex items-center justify-center h-[52px] w-[52px] rounded-xl bg-blue-600 text-white transition hover:bg-blue-700 disabled:opacity-50 disabled:hover:bg-blue-600 shrink-0"
                    >
                        <FiSend size={18} className="mr-1 mt-0.5" />
                    </button>
                </div>
            </form>
        </div>
    );
};
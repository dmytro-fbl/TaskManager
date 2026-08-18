import React, { useState } from "react";
import { useQuery, useMutation, useLazyQuery } from "@apollo/client/react";
import { FiMessageSquare, FiSend, FiCornerDownRight, FiEdit2, FiTrash2, FiEye, FiEyeOff, FiClock } from "react-icons/fi";
import { getFriendlyErrorMessage } from "../../../utils/errorHandler";

import { GET_TASK_COMMENTS, GET_TASK_COMMENT_VERSIONS } from "../../../graphql/queries/task/taskComment/taskCommentQueries";
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

type TaskCommentVersion = {
    id: string;
    commentId: string;
    previousBody: string;
    changedAt: string;
};

type TaskCommentsResponse = {
    taskComments: TaskComment[];
};

type VersionsResponse = {
    taskCommentVersions: TaskCommentVersion[];
};

export const TaskCommentsSection: React.FC<Props> = ({ taskId, memberships, currentUserId }) => {
    const [newComment, setNewComment] = useState("");
    const [replyingTo, setReplyingTo] = useState<string | null>(null);
    const [error, setError] = useState("");

    const [editingId, setEditingId] = useState<string | null>(null);
    const [editBody, setEditBody] = useState("");

    const [revealedDeletedIds, setRevealedDeletedIds] = useState<string[]>([]);
    const [historyOpenId, setHistoryOpenId] = useState<string | null>(null);

    const { data, loading, refetch } = useQuery<TaskCommentsResponse>(GET_TASK_COMMENTS, {
        variables: { taskId },
        fetchPolicy: "cache-and-network",
    });

    const [fetchVersions, { data: versionsData, loading: fetchingVersions }] = useLazyQuery<VersionsResponse>(GET_TASK_COMMENT_VERSIONS, {
        fetchPolicy: "network-only"
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
        setHistoryOpenId(null); 
    };

    const handleDelete = async (commentId: string) => {
        if (window.confirm("Ви впевнені, що хочете видалити цей коментар?")) {
            await deleteComment({
                variables: { input: { commentId } }
            });
            setHistoryOpenId(null);
            setRevealedDeletedIds(prev => prev.filter(id => id !== commentId));
        }
    };

    const toggleDeletedVisibility = (commentId: string) => {
        setRevealedDeletedIds(prev => 
            prev.includes(commentId) 
                ? prev.filter(id => id !== commentId) 
                : [...prev, commentId]
        );
    };

    const toggleHistory = (commentId: string) => {
        if (historyOpenId === commentId) {
            setHistoryOpenId(null); 
        } else {
            setHistoryOpenId(commentId); 
            fetchVersions({ variables: { commentId } }); 
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
                    comments.map((comment) => {
                        const isDeletedRevealed = revealedDeletedIds.includes(comment.id);
                        const isHistoryOpen = historyOpenId === comment.id;

                        return (
                            <div 
                                key={comment.id} 
                                className={`p-4 rounded-xl border transition ${
                                    comment.parentCommentId 
                                        ? "ml-8 bg-gray-50 border-gray-100" 
                                        : "bg-white border-gray-200"
                                }`}
                            >
                                <div className="flex justify-between items-start mb-2">
                                    <div className="flex items-center gap-2 flex-wrap">
                                        <span className="font-semibold text-sm text-[#1f2937]">
                                            {getAuthorName(comment.authorId)}
                                        </span>
                                        <span className="text-[11px] text-gray-400">
                                            {formatTime(comment.createdAt)}
                                        </span>
                                        
                                        {comment.isEdited && !comment.isDeleted && editingId !== comment.id && (
                                            <button 
                                                onClick={() => toggleHistory(comment.id)}
                                                className={`text-[10px] italic flex items-center gap-1 transition ${
                                                    isHistoryOpen ? "text-blue-600 font-semibold" : "text-gray-400 hover:text-blue-500"
                                                }`}
                                            >
                                                <FiClock size={10} />
                                                {isHistoryOpen ? "Сховати історію" : "(редаговано)"}
                                            </button>
                                        )}
                                    </div>
                                </div>
                                
                                <div className="text-sm text-[#374151] whitespace-pre-wrap mt-1">
                                    {comment.isDeleted ? (
                                        <div className="space-y-2">
                                            <button 
                                                onClick={() => toggleDeletedVisibility(comment.id)}
                                                className="flex items-center gap-1.5 text-[11px] font-bold text-red-500 bg-red-50 border border-red-100 px-2 py-1 rounded hover:bg-red-100 transition"
                                            >
                                                {isDeletedRevealed ? <FiEyeOff size={12} /> : <FiEye size={12} />}
                                                {isDeletedRevealed ? "Сховати видалений коментар" : "Коментар видалено (натисніть, щоб переглянути)"}
                                            </button>
                                            
                                            {isDeletedRevealed && (
                                                <div className="text-gray-400 opacity-70 p-3 bg-gray-50 rounded-lg border border-gray-200">
                                                    {comment.body}
                                                </div>
                                            )}
                                        </div>
                                    ) : editingId === comment.id ? (
                                        // Форма редагування
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
                                        <div>{comment.body}</div>
                                    )}
                                </div>

                                {/* БЛОК ІСТОРІЇ РЕДАГУВАНЬ */}
                                {isHistoryOpen && !comment.isDeleted && (
                                    <div className="mt-4 pl-4 border-l-2 border-blue-200 space-y-3">
                                        <div className="text-[11px] font-bold text-gray-400 uppercase tracking-wider">
                                            Попередні версії:
                                        </div>
                                        {fetchingVersions ? (
                                            <div className="text-xs text-gray-400">Завантаження історії...</div>
                                        ) : versionsData?.taskCommentVersions && versionsData.taskCommentVersions.length > 0 ? (
                                            versionsData.taskCommentVersions.map(version => (
                                                <div key={version.id} className="bg-gray-50 p-3 rounded-lg border border-gray-100 shadow-sm">
                                                    <div className="text-[10px] text-gray-400 mb-1.5 flex items-center gap-1">
                                                        <FiClock size={10} /> Було до {formatTime(version.changedAt)}
                                                    </div>
                                                    <div className="text-xs text-gray-500 whitespace-pre-wrap">
                                                        {version.previousBody}
                                                    </div>
                                                </div>
                                            ))
                                        ) : (
                                            <div className="text-xs text-gray-400 italic">Історія порожня.</div>
                                        )}
                                    </div>
                                )}

                                {/* КНОПКИ ДІЙ */}
                                {!comment.isDeleted && editingId !== comment.id && (
                                    <div className="flex items-center gap-4 mt-3 pt-3 border-t border-gray-100/50">
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
                                                        setHistoryOpenId(null); 
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
                        );
                    })
                )}
            </div>

            {/* Форма вводу */}
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
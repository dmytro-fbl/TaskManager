import React from "react";

interface ErrorMessageProps {
  message?: string | null;
}

export default function ErrorMessage({ message }: ErrorMessageProps) {
  if (!message) return null;

  return (
    <div className="bg-danger-bg border border-danger-border text-danger p-3 rounded-lg text-sm flex items-center gap-2 mb-4">
      <span className="font-medium">{message}</span>
    </div>
  );
}
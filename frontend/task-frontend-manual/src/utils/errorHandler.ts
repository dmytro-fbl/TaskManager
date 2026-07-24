export interface CustomApolloError {
  message: string;
  networkError?: any;
  graphQLErrors?: ReadonlyArray<{ message: string }>;
}

export const getFriendlyErrorMessage = (error: CustomApolloError | undefined): string | null => {
    if (!error) return null;

    if (error.networkError) {
        return "Не вдалося зв'язатись з сервером. Перевірте підключення до інтернету або спробуйте пізніше";
    }

    if (error.graphQLErrors && error.graphQLErrors.length > 0) {
        return error.graphQLErrors[0].message;
    }


    if (error.message.includes('Failed to fetch')) {
        return "Сервер тимчасово недоступний. Спробуйте пізніше.";
    }

    return error.message || "Сталася невідома помилка. Спробуйте ще раз.";
};

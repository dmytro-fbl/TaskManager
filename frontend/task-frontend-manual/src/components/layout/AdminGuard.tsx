import { Navigate, Outlet } from "react-router-dom";
import { useQuery } from "@apollo/client/react";

import { GetMeData } from "./Header";
import { GET_ME_QUERY } from "../../graphql/queries/autorization/autorizationQueries";



export default function Adminguard(){
    const { data, loading, error } = useQuery<GetMeData>(GET_ME_QUERY);

    if(loading){
        return (
      <div className="min-h-screen flex items-center justify-center bg-bg-main">
        <div className="animate-spin rounded-full h-10 w-10 border-4 border-gray-200 border-t-primary"></div>
      </div>
    );
    }

    if (error || !data?.me?.isAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}